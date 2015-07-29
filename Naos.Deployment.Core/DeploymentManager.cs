// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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

        private readonly string messageBusPersistenceConnectionString;

        private readonly ICollection<string> packageIdsToIgnoreDuringTerminationSearch;

        private readonly LogProcessorSettings handlerHarnessLogProcessorSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentManager"/> class.
        /// </summary>
        /// <param name="tracker">Tracker of computing infrastructure.</param>
        /// <param name="cloudManager">Manager of the cloud infrastructure (wraps custom cloud interactions).</param>
        /// <param name="packageManager">Proxy to retrieve packages.</param>
        /// <param name="certificateRetriever">Manager of certificates (get passwords and file bytes by name).</param>
        /// <param name="setupStepFactorySettings">Settings for the setup step factory.</param>
        /// <param name="defaultDeploymentConfig">Default deployment configuration to substitute the values for any nulls.</param>
        /// <param name="handlerHarnessPackageDescriptionWithOverrides">The package that will be used as a harness for the NAOS.MessageBus handlers that are being deployed.</param>
        /// <param name="handlerHarnessLogProcessorSettings">Log processor settings to be used when deploying a message bus handler harness.</param>
        /// <param name="messageBusPersistenceConnectionString">Connection string to the message bus harness.</param>
        /// <param name="packageIdsToIgnoreDuringTerminationSearch">List of package IDs to exclude during replacement search.</param>
        /// <param name="announcer">Callback to get status messages through process.</param>
        public DeploymentManager(
            ITrackComputingInfrastructure tracker,
            IManageCloudInfrastructure cloudManager,
            IManagePackages packageManager,
            IGetCertificates certificateRetriever,
            SetupStepFactorySettings setupStepFactorySettings,
            DeploymentConfiguration defaultDeploymentConfig,
            PackageDescriptionWithOverrides handlerHarnessPackageDescriptionWithOverrides,
            LogProcessorSettings handlerHarnessLogProcessorSettings,
            string messageBusPersistenceConnectionString,
            ICollection<string> packageIdsToIgnoreDuringTerminationSearch,
            Action<string> announcer)
        {
            this.tracker = tracker;
            this.cloudManager = cloudManager;
            this.packageManager = packageManager;
            this.defaultDeploymentConfig = defaultDeploymentConfig;
            this.handlerHarnessPackageDescriptionWithOverrides = handlerHarnessPackageDescriptionWithOverrides;
            this.handlerHarnessLogProcessorSettings = handlerHarnessLogProcessorSettings;
            this.messageBusPersistenceConnectionString = messageBusPersistenceConnectionString;
            this.packageIdsToIgnoreDuringTerminationSearch = packageIdsToIgnoreDuringTerminationSearch;
            this.announce = announcer;
            this.setupStepFactory = new SetupStepFactory(setupStepFactorySettings, certificateRetriever, packageManager);
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
            var deploymentFileSearchPattern = string.Format(".config/{0}/DeploymentConfigurationWithStrategies.json", environment);

            this.announce("Extracting deployment configuration(s) for specified environment from packages (if present).");
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
            var createdInstanceDescription = this.cloudManager.CreateNewInstance(environment, instanceName, configToCreateWith);

            this.announce(
                "Created new instance (waiting for Administrator password to be available); ID: "
                + createdInstanceDescription.Id + ", Private IP:" + createdInstanceDescription.PrivateIpAddress);
            var machineManager = this.GetMachineManagerForInstance(createdInstanceDescription);

            // this is necessary for finishing start up items, might have to try a few times until WinRM is available...
            this.announce("Rebooting new instance to finalize any items from user data setup.");
            this.RebootInstance(machineManager);

            // get all message bus handler initializations to know if we need a handler.
            var packagesWithMessageBusInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .WhereContainsInitializationStrategyOf<InitializationStrategyMessageBusHandler>();

            var messageBusInitializations =
                packagesWithMessageBusInitializations
                    .GetInitializationStrategiesOf<InitializationStrategyMessageBusHandler>();

            // make sure we're not already deploying the package ('server/host/schedule manager' is only scenario of this right now...)
            var notAlreadyDeployingTheSamePackageAsHandlersUse =
                packagedDeploymentConfigsWithDefaultsAndOverrides.All(
                    _ => _.Package.PackageDescription.Id != this.handlerHarnessPackageDescriptionWithOverrides.Id);

            if (messageBusInitializations.Any() && notAlreadyDeployingTheSamePackageAsHandlersUse)
            {
                this.announce("Including MessageBusHandlerHarness in deployment since MessageBusHandlers are being deployed.");

                var itsConfigOverridesForHandlers = new List<ItsConfigOverride>();

                this.announce(
                    "Adding any Its.Config overrides AND/OR embedded Its.Config files from Message Handler package into Its.Config overrides of the Harness.");
                foreach (var packageWithMessageBusInitializations in packagesWithMessageBusInitializations)
                {
                    itsConfigOverridesForHandlers.AddRange(packageWithMessageBusInitializations.ItsConfigOverrides ?? new List<ItsConfigOverride>());

                    var itsConfigEnvironmentFolderPattern = string.Format(".config/{0}/", environment);
                    var itsConfigFilesFromPackage =
                        this.packageManager.GetMultipleFileContentsFromPackageAsStrings(
                            packageWithMessageBusInitializations.Package,
                            itsConfigEnvironmentFolderPattern);

                    itsConfigOverridesForHandlers.AddRange(
                        itsConfigFilesFromPackage.Select(
                            _ =>
                            new ItsConfigOverride
                                {
                                    FileNameWithoutExtension = Path.GetFileNameWithoutExtension(_.Key),
                                    FileContentsJson = _.Value
                                }));
                }

                var harnessPackagedConfig = this.GetMessageBusHarnessPackagedConfig(
                    instanceName,
                    messageBusInitializations,
                    itsConfigOverridesForHandlers,
                    configToCreateWith);

                packagedDeploymentConfigsWithDefaultsAndOverrides.Add(
                    harnessPackagedConfig);
            }

            foreach (var packagedConfig in packagedDeploymentConfigsWithDefaultsAndOverrides)
            {
                var setupSteps = this.setupStepFactory.GetSetupSteps(packagedConfig, environment);
                this.announce(
                    "Running setup actions for package: "
                    + packagedConfig.Package.PackageDescription.GetIdDotVersionString());
                foreach (var setupStep in setupSteps)
                {
                    this.announce("  - " + setupStep.Description);
                    setupStep.SetupAction(machineManager);
                }
             
                // Mark the instance as having the successfully deployed packages
                this.tracker.ProcessDeployedPackage(
                    environment,
                    createdInstanceDescription.Id,
                    packagedConfig.Package.PackageDescription);
            }

            // get all web initializations to update any DNS entries on the public IP address.
            var webInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .GetInitializationStrategiesOf<InitializationStrategyWeb>();

            this.announce("Updating DNS for web initializations (if applicable)");
            foreach (var webInitialization in webInitializations)
            {
                var ipAddress = createdInstanceDescription.PublicIpAddress
                                ?? createdInstanceDescription.PrivateIpAddress;
                var dns = webInitialization.PrimaryDns;

                this.announce(string.Format(" - Pointing {0} at {1}.", dns, ipAddress));

                this.cloudManager.UpsertDnsEntry(
                    environment,
                    createdInstanceDescription.Location,
                    dns,
                    new[] { ipAddress });
            }

            // get all initializations to update any private DNS entries on the private IP address.
            this.announce("Updating private DNS for all initializations (if applicable)");
            var allInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .GetInitializationStrategiesOf<InitializationStrategyBase>();
            foreach (var initialization in allInitializations)
            {
                var privateDnsEntries = initialization.PrivateDnsEntries ?? new List<string>();
                foreach (var privateDnsEntry in privateDnsEntries)
                {
                    var ipAddress = createdInstanceDescription.PrivateIpAddress;
                    this.announce(string.Format(" - Pointing {0} at {1}.", privateDnsEntry, ipAddress));
                    this.cloudManager.UpsertDnsEntry(
                        environment,
                        createdInstanceDescription.Location,
                        privateDnsEntry,
                        new[] { ipAddress });
                }
            }

            this.announce("Finished deployment.");
        }

        private ICollection<PackagedDeploymentConfiguration> GetPackagedDeploymentConfigurations(
            ICollection<PackageDescriptionWithOverrides> packagesToDeploy,
            string deploymentFileSearchPattern)
        {
            var packagedDeploymentConfigs = packagesToDeploy.Select(
                packageDescriptionWithOverrides =>
                    {
                        // decide whether we need to get all of the dependencies or just the normal package
                        // currently this is for message bus handlers since web services already include all assemblies...
                        var bundleAllDependencies = packageDescriptionWithOverrides.InitializationStrategies != null
                                                    && packageDescriptionWithOverrides
                                                           .GetInitializationStrategiesOf<InitializationStrategyMessageBusHandler>().Any();

                        var package = this.packageManager.GetPackage(packageDescriptionWithOverrides, bundleAllDependencies);
                        var deploymentConfigJson =
                            this.packageManager.GetMultipleFileContentsFromPackageAsStrings(
                                package,
                                deploymentFileSearchPattern).Select(_ => _.Value).SingleOrDefault();

                        var deploymentConfig =
                            Serializer.Deserialize<DeploymentConfigurationWithStrategies>(deploymentConfigJson);

                        // take overrides if present, otherwise take existing, otherwise take empty
                        var initializationStrategies = packageDescriptionWithOverrides.InitializationStrategies != null
                                                       && packageDescriptionWithOverrides.InitializationStrategies.Count
                                                       > 0
                                                           ? packageDescriptionWithOverrides.InitializationStrategies
                                                           : (deploymentConfig
                                                              ?? new DeploymentConfigurationWithStrategies())
                                                                 .InitializationStrategies
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

        private PackagedDeploymentConfiguration GetMessageBusHarnessPackagedConfig(string instanceName, ICollection<InitializationStrategyMessageBusHandler> messageBusInitializations, ICollection<ItsConfigOverride> itsConfigOverrides, DeploymentConfiguration configToCreateWith)
        {
            // TODO:    Maybe this should be exclusively done with that provided package and 
            // TODO:        only update the private channel to monitor and directory of packages...

            // Create a new list to use for the overrides of the handler harness deployment
            var itsConfigOverridesToUse = new List<ItsConfigOverride>();
            if (itsConfigOverrides != null)
            {
                // merge in any ItsConfig overrides supplied with handler packages
                itsConfigOverridesToUse.AddRange(itsConfigOverrides);
            }

            if (this.handlerHarnessPackageDescriptionWithOverrides.ItsConfigOverrides != null)
            {
                // merge in any overrides specified with the handler package
                itsConfigOverridesToUse.AddRange(this.handlerHarnessPackageDescriptionWithOverrides.ItsConfigOverrides);
            }

            var messageBusHandlerPackage =
                this.packageManager.GetPackage(this.handlerHarnessPackageDescriptionWithOverrides);

            var channelsToMonitor = messageBusInitializations.SelectMany(_ => _.ChannelsToMonitor).Distinct().ToList();

            var executorRoleSettings = new[]
                                           {
                                               new MessageBusHarnessRoleSettingsExecutor
                                                   {
                                                       ChannelsToMonitor =
                                                           channelsToMonitor,
                                                       HandlerAssemblyPath =
                                                           SetupStepFactory
                                                           .RootDeploymentPath,
                                                       WorkerCount =
                                                           configToCreateWith
                                                               .InstanceType
                                                               .VirtualCores ?? 1,
                                                       PollingTimeSpan =
                                                           TimeSpan.FromMinutes(1),
                                                   }
                                           };

            var messageBusHandlerSettings = new MessageBusHarnessSettings
                                                {
                                                    PersistenceConnectionString = this.messageBusPersistenceConnectionString,
                                                    RoleSettings = executorRoleSettings,
                                                    LogProcessorSettings = this.handlerHarnessLogProcessorSettings
                                                };

            // add the override that will activate the harness in executor mode.
            var messageBusHandlerSettingsJson = Serializer.Serialize(messageBusHandlerSettings);
            itsConfigOverridesToUse.Add(
                new ItsConfigOverride
                    {
                        FileNameWithoutExtension = "MessageBusHarnessSettings",
                        FileContentsJson = messageBusHandlerSettingsJson
                    });

            var harnessPackagedConfig = new PackagedDeploymentConfiguration
                                            {
                                                DeploymentConfiguration =
                                                    configToCreateWith,
                                                Package = messageBusHandlerPackage,
                                                ItsConfigOverrides =
                                                    itsConfigOverridesToUse,
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
            var privateKey = this.tracker.GetPrivateKeyOfInstanceById(createdInstanceDescription.Environment, createdInstanceDescription.Id);
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
            var packagesToIgnore = this.packageIdsToIgnoreDuringTerminationSearch.Select(_ => new PackageDescription { Id = _ }).ToList();

            // get aws instance object by name (from the AWS object tracking storage)
            var packagesToCheckFor = packagesToDeploy.Except(packagesToIgnore, new PackageDescriptionIdOnlyEqualityComparer()).ToList();
            var instancesMatchingPackagesAllEnvironments =
                this.tracker.GetInstancesByDeployedPackages(environment, packagesToCheckFor).ToList();
            var instancesWithMatchingEnvironmentAndPackages =
                instancesMatchingPackagesAllEnvironments.Where(_ => _.Environment == environment).ToList();

            // confirm that terminating the instances will not take down any packages that aren't getting re-deployed...
            var deployedPackagesToCheck =
                instancesWithMatchingEnvironmentAndPackages.SelectMany(_ => _.DeployedPackages)
                    .Except(packagesToIgnore, new PackageDescriptionIdOnlyEqualityComparer())
                    .ToList();
            if (deployedPackagesToCheck.Except(packagesToDeploy, new PackageDescriptionIdOnlyEqualityComparer()).Any())
            {
                var deployedIdList = string.Join(",", deployedPackagesToCheck.Select(_ => _.Id));
                var deployingIdList = string.Join(",", packagesToDeploy.Select(_ => _.Id));
                throw new DeploymentException(
                    "Cannot proceed because taking down the instances of requested packages will take down packages not getting redeployed; Running: "
                    + deployedIdList + " Deploying: " + deployingIdList);
            }

            // terminate instance(s) if necessary (if it exists)
            foreach (var instanceDescription in instancesWithMatchingEnvironmentAndPackages)
            {
                this.announce("Terminating instance; ID: " + instanceDescription.Id + ", Name: " + instanceDescription.Name);
                this.cloudManager.Terminate(environment, instanceDescription.Id, instanceDescription.Location, true);
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
            var privateKey = this.tracker.GetPrivateKeyOfInstanceById(instanceToSearchFor.Environment, instanceToSearchFor.Id);
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
