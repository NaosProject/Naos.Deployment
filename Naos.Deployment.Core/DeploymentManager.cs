// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Naos.AWS.Core;
    using Naos.Deployment.Contract;
    using Naos.MessageBus.HandlingContract;
    using Naos.WinRM;

    /// <inheritdoc />
    public class DeploymentManager : IManageDeployments
    {
        private readonly ITrackComputingInfrastructure tracker;

        private readonly IManageCloudInfrastructure cloudManager;

        private readonly IManagePackages packageManager;

        private readonly DeploymentConfiguration defaultDeploymentConfig;

        private readonly PackageDescriptionWithOverrides handlerHarnessPackageDescriptionWithOverrides;

        private readonly Action<string> announce;

        private readonly SetupStepFactory setupStepFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentManager"/> class.
        /// </summary>
        /// <param name="tracker">Tracker of computing infrastructure.</param>
        /// <param name="cloudManager">Manager of the cloud infrastructure (wraps custom cloud interactions).</param>
        /// <param name="packageManager">Proxy to retrieve packages.</param>
        /// <param name="certificateRetriever">Manager of certificates (get passwords and file bytes by name).</param>
        /// <param name="defaultDeploymentConfig">Default deployment configuration to substitute the values for any nulls.</param>
        /// <param name="handlerHarnessPackageDescriptionWithOverrides">The package that will be used as a harness for the NAOS.MessageBus handlers that are being deployed.</param>
        /// <param name="announcer">Callback to get status messages through process.</param>
        public DeploymentManager(
            ITrackComputingInfrastructure tracker,
            IManageCloudInfrastructure cloudManager,
            IManagePackages packageManager,
            IGetCertificates certificateRetriever,
            DeploymentConfiguration defaultDeploymentConfig,
            PackageDescriptionWithOverrides handlerHarnessPackageDescriptionWithOverrides,
            Action<string> announcer)
        {
            this.tracker = tracker;
            this.cloudManager = cloudManager;
            this.packageManager = packageManager;
            this.setupStepFactory = new SetupStepFactory(certificateRetriever);
            this.defaultDeploymentConfig = defaultDeploymentConfig;
            this.handlerHarnessPackageDescriptionWithOverrides = handlerHarnessPackageDescriptionWithOverrides;
            this.announce = announcer;
        }

        /// <inheritdoc />
        public void DeployPackages(
            ICollection<PackageDescriptionWithOverrides> packagesToDeploy,
            string environment,
            string instanceName,
            DeploymentConfiguration deploymentConfigOverride = null)
        {
            if (packagesToDeploy == null)
            {
                packagesToDeploy = new List<PackageDescriptionWithOverrides>();
            }

            if (packagesToDeploy.Count > 1)
            {
                throw new NotSupportedException("Currently deploying multiple packages at once is not supported.");
            }

            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = string.Join("---", packagesToDeploy.Select(_ => _.Id.Replace(".", "-")).ToArray());
            }

            this.TerminateInstancesBeingReplaced(packagesToDeploy.WithoutStrategies(), environment);

            // get the NuGet package to push to instance AND crack open for Its.Config deployment file
            this.announce(
                "Downloading packages that are to be deployed; IDs: "
                + string.Join(",", packagesToDeploy.Select(_ => _.Id)));

            // get deployment details from Its.Config in the package
            var deploymentFileSearchPattern = string.Format(".config/{0}/Deployment.json", environment);

            var packagedDeploymentConfigs = this.GetPackagedDeploymentConfigurations(packagesToDeploy, deploymentFileSearchPattern);

            // apply default values to any nulls
            this.announce("Applying default deployment configuration options.");
            var packagedConfigsWithDefaults = packagedDeploymentConfigs.ApplyDefaults(this.defaultDeploymentConfig);

            // flatten configs into a single config to deploy onto an instance
            this.announce("Flattening multiple deployment configurations.");
            var flattenedConfig = packagedConfigsWithDefaults.Select(_ => _.DeploymentConfiguration).ToList().Flatten();

            // apply overrides
            this.announce("Applying applicable overrides to the flattened deployment configuration.");
            var overriddenConfig = flattenedConfig.ApplyOverrides(deploymentConfigOverride);

            // set config to use for creation
            var configToCreateWith = overriddenConfig;

            // apply newly constructed configs across all configs
            var packagedDeploymentConfigsWithDefaultsAndOverrides =
                packagedDeploymentConfigs.OverrideDeploymentConfig(configToCreateWith);

            // create new aws instance(s)
            this.announce("Creating new instance; Name: " + instanceName);
            var createdInstanceDescription = this.cloudManager.CreateNewInstance(instanceName, environment, configToCreateWith);

            this.announce(
                "Created new instance (waiting for Administrator password to be available); ID: "
                + createdInstanceDescription.Id + ", Private IP:" + createdInstanceDescription.PrivateIpAddress
                + ", Private DNS: " + createdInstanceDescription.PrivateDns);
            var machineManager = this.GetMachineManagerForInstance(createdInstanceDescription);

            // this is necessary for finishing start up items, might have to try a few times until WinRM is available...
            this.announce("Rebooting new instance to finalize any items from user data setup.");
            this.RebootInstance(machineManager);

            // get all message bus handler initializations to know if we need a handler.
            var messageBusInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .GetInitializationStrategiesOf<InitializationStrategyMessageBusHandler>();

            if (messageBusInitializations.Any())
            {
                // if we have any message bus handlers that are being deployed then we need to also deploy the harness
                var harnessPackagedConfig = this.GetMessageBusHarnessPackagedConfig(
                    instanceName,
                    messageBusInitializations,
                    configToCreateWith);

                packagedDeploymentConfigsWithDefaultsAndOverrides.Add(
                    harnessPackagedConfig);
            }

            foreach (var packagedConfig in packagedDeploymentConfigsWithDefaultsAndOverrides)
            {
                var setupSteps = this.setupStepFactory.GetSetupSteps(packagedConfig, environment);
                this.announce("Running setup actions for package ID: " + packagedConfig.Package.PackageDescription.Id);
                foreach (var setupStep in setupSteps)
                {
                    setupStep.SetupAction(machineManager);
                }
             
                // Mark the instance as having the successfully deployed packages
                this.tracker.ProcessDeployedPackage(
                    createdInstanceDescription.Id,
                    packagedConfig.Package.PackageDescription);
                this.announce("Finished deployment.");
            }

            // get all web initializations to update any DNS entries on the public IP address.
            var webInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .GetInitializationStrategiesOf<InitializationStrategyWeb>();

            foreach (var webInitialization in webInitializations)
            {
                this.cloudManager.UpsertDnsEntry(
                    createdInstanceDescription.Location,
                    webInitialization.PrimaryDns,
                    new[] { createdInstanceDescription.PublicIpAddress });
            }
        }

        private ICollection<PackagedDeploymentConfiguration> GetPackagedDeploymentConfigurations(
            ICollection<PackageDescriptionWithOverrides> packagesToDeploy,
            string deploymentFileSearchPattern)
        {
            var packagedDeploymentConfigs = packagesToDeploy.Select(
                packageDescriptionWithOverrides =>
                    {
                        var package = this.packageManager.GetPackage(packageDescriptionWithOverrides);
                        var deploymentConfig =
                            Serializer.Deserialize<DeploymentConfigurationWithStrategies>(
                                this.packageManager.GetFileContentsFromPackage(package, deploymentFileSearchPattern));

                        // take overrides if present, otherwise take existing, otherwise take empty
                        var initializationStrategies = packageDescriptionWithOverrides.InitializationStrategies != null
                                                       && packageDescriptionWithOverrides.InitializationStrategies.Count
                                                       > 0
                                                           ? packageDescriptionWithOverrides.InitializationStrategies
                                                           : deploymentConfig.InitializationStrategies
                                                             ?? new List<InitializationStrategyBase>();

                        var newItem = new PackagedDeploymentConfiguration
                                          {
                                              Package = package,
                                              DeploymentConfiguration = deploymentConfig,
                                              InitializationStrategies =
                                                  initializationStrategies,
                                              ItsConfigOverrides =
                                                  packageDescriptionWithOverrides
                                                  .ItsConfigOverrides,
                                          };
                        return newItem;
                    }).ToList();

            return packagedDeploymentConfigs;
        }

        private PackagedDeploymentConfiguration GetMessageBusHarnessPackagedConfig(
            string instanceName,
            ICollection<InitializationStrategyMessageBusHandler> messageBusInitializations,
            DeploymentConfiguration configToCreateWith)
        {
            var messageBusHandlerPackage =
                this.packageManager.GetPackage(this.handlerHarnessPackageDescriptionWithOverrides);

            var privateQueueName = instanceName;
            var channelsToMonitor =
                new[] { privateQueueName }.Concat(messageBusInitializations.SelectMany(_ => _.ChannelsToMonitor)).ToList();

            var messageBusHandlerSettings = new MessageBusHarnessRoleSettingsExecutor
            {
                ChannelsToMonitor = channelsToMonitor,
                HandlerAssemblyPath =
                    SetupStepFactory.RootDeploymentPath,
                WorkerCount =
                    configToCreateWith.InstanceType
                        .VirtualCores ?? 1,
                PollingTimeSpan =
                    TimeSpan.FromMinutes(1)
            };

            var messageBusHandlerSettingsJson = Serializer.Serialize(messageBusHandlerSettings);
            var harnessPackagedConfig = new PackagedDeploymentConfiguration
                                            {
                                                DeploymentConfiguration =
                                                    configToCreateWith,
                                                Package = messageBusHandlerPackage,
                                                ItsConfigOverrides =
                                                    new[]
                                                        {
                                                            new ItsConfigOverride
                                                                {
                                                                    FileNameWithoutExtension
                                                                        =
                                                                        "MessageBusHarnessSettings",
                                                                    FileContentsJson
                                                                        =
                                                                        messageBusHandlerSettingsJson
                                                                }
                                                        }
                                                    .Concat(
                                                        this
                                                    .handlerHarnessPackageDescriptionWithOverrides
                                                    .ItsConfigOverrides).ToList(),
                                                InitializationStrategies =
                                                    this
                                                    .handlerHarnessPackageDescriptionWithOverrides
                                                    .InitializationStrategies,
                                            };

            // apply instance name replacement if applicable on DNS
            foreach (
                var initializationStrategy in
                    harnessPackagedConfig.GetInitializationStrategiesOf<InitializationStrategyWeb>())
            {
                initializationStrategy.PrimaryDns = initializationStrategy.PrimaryDns.Replace(
                    "{instanceName}",
                    instanceName);
            }

            return harnessPackagedConfig;
        }

        private void RebootInstance(MachineManager machineManager)
        {
            var sleepTimeInSeconds = 1d;
            var rebootCallSucceeded = false;
            while (!rebootCallSucceeded)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                try
                {
                    machineManager.Reboot();
                    rebootCallSucceeded = true;
                }
                catch (Exception)
                {
                    /* no-op */
                }
            }

            // TODO: move to machineManager.BlockUntilAvailable(TimeSpan.Zero);
            this.announce("Waiting for machine to come back up from reboot.");
            sleepTimeInSeconds = 10d;
            var reachable = false;
            while (!reachable)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                try
                {
                    var notNeededResults = machineManager.RunScript(@"{ ls C:\Windows }");
                    reachable = true;
                }
                catch (Exception)
                {
                    /* no-op */
                }
            }
        }

        private MachineManager GetMachineManagerForInstance(InstanceDescription createdInstanceDescription)
        {
            string adminPassword = null;
            var sleepTimeInSeconds = 30d;
            var privateKey = this.tracker.GetPrivateKeyOfInstanceById(createdInstanceDescription.Id);
            while (adminPassword == null)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                try
                {
                    adminPassword = this.cloudManager.GetAdministratorPasswordForInstance(
                        createdInstanceDescription,
                        privateKey);
                }
                catch (NullPasswordDataException)
                {
                    // No-op
                }
            }

            // finalize instance creation (WinRM reboot, etc.)
            var machineManager = new MachineManager(
                createdInstanceDescription.PrivateIpAddress,
                "Administrator",
                MachineManager.ConvertStringToSecureString(adminPassword),
                true);
            return machineManager;
        }

        private void TerminateInstancesBeingReplaced(ICollection<PackageDescription> packagesToDeploy, string environment)
        {
            // get aws instance object by name (from the AWS object tracking storage)
            var instancesMatchingPackagesAllEnvironments = this.tracker.GetInstancesByDeployedPackages(packagesToDeploy);
            var instancesWithMatchingEnvironmentAndPackages =
                instancesMatchingPackagesAllEnvironments.Where(_ => _.Environment == environment).ToList();

            // confirm that terminating the instances will not take down any packages that aren't getting re-deployed...
            var deployedPackages = instancesWithMatchingEnvironmentAndPackages.SelectMany(_ => _.DeployedPackages).ToList();
            if (deployedPackages.Except(packagesToDeploy, new PackageDescriptionIdOnlyEqualityComparer()).Any())
            {
                var deployedIdList = string.Join(",", deployedPackages.Select(_ => _.Id));
                var deployingIdList = string.Join(",", packagesToDeploy.Select(_ => _.Id));
                throw new DeploymentException(
                    "Cannot proceed because taking down the instances of requested packages will take down packages not getting redeployed; Running: "
                    + deployedIdList + " Deploying: " + deployingIdList);
            }

            // terminate instance(s) if necessary (if it exists)
            foreach (var instanceDescription in instancesWithMatchingEnvironmentAndPackages)
            {
                this.announce("Terminating instance; ID: " + instanceDescription.Id + ", Name: " + instanceDescription.Name);
                this.cloudManager.Terminate(instanceDescription.Id, instanceDescription.Location, true);
            }
        }

        /// <summary>
        /// Gets the password for the instance.
        /// </summary>
        /// <param name="instanceToSearchFor">Instance to find the password for.</param>
        /// <returns>Password for instance.</returns>
        public string GetPassword(InstanceDescription instanceToSearchFor)
        {
            string adminPassword = null;
            var sleepTimeInSeconds = .25;
            var privateKey = this.tracker.GetPrivateKeyOfInstanceById(instanceToSearchFor.Id);
            while (adminPassword == null)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 2;
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));
                adminPassword = this.cloudManager.GetAdministratorPasswordForInstance(instanceToSearchFor, privateKey);
            }

            return adminPassword;
        }
    }
}
