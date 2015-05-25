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
    using Naos.WinRM;

    /// <inheritdoc />
    public class DeploymentManager : IManageDeployments
    {
        private readonly ITrackComputingInfrastructure tracker;

        private readonly IManageCloudInfrastructure cloudManager;

        private readonly IManagePackages packageManager;

        private readonly DeploymentConfiguration defaultDeploymentConfig;

        private readonly IGetCertificates certificateRetriever;

        private readonly Action<string> announce;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentManager"/> class.
        /// </summary>
        /// <param name="tracker">Tracker of computing infrastructure.</param>
        /// <param name="cloudManager">Manager of the cloud infrastructure (wraps custom cloud interactions).</param>
        /// <param name="packageManager">Proxy to retrieve packages.</param>
        /// <param name="certificateRetriever">Manager of certificates (get passwords and file bytes by name).</param>
        /// <param name="defaultDeploymentConfig">Default deployment configuration to substitute the values for any nulls.</param>
        /// <param name="announcer">Callback to get status messages through process.</param>
        public DeploymentManager(
            ITrackComputingInfrastructure tracker,
            IManageCloudInfrastructure cloudManager,
            IManagePackages packageManager,
            IGetCertificates certificateRetriever,
            DeploymentConfiguration defaultDeploymentConfig,
            Action<string> announcer)
        {
            this.tracker = tracker;
            this.cloudManager = cloudManager;
            this.packageManager = packageManager;
            this.certificateRetriever = certificateRetriever;
            this.defaultDeploymentConfig = defaultDeploymentConfig;
            this.announce = announcer;
        }

        /// <inheritdoc />
        public void DeployPackages(
            ICollection<PackageDescription> packagesToDeploy,
            string environment,
            string instanceName,
            DeploymentConfiguration deploymentConfigOverride = null)
        {
            if (packagesToDeploy == null)
            {
                packagesToDeploy = new List<PackageDescription>();
            }

            if (packagesToDeploy.Count > 1)
            {
                throw new NotSupportedException("Currently deploying multiple packages at once is not supported.");
            }

            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = string.Join("---", packagesToDeploy.Select(_ => _.Id.Replace(".", "-")).ToArray());
            }

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
                this.announce(
                    "Terminating instance; ID: " + instanceDescription.Id + ", Name: " + instanceDescription.Name);
                this.cloudManager.Terminate(instanceDescription.Id, instanceDescription.Location, true);
            }

            // get the NuGet package to push to instance AND crack open for Its.Config deployment file
            this.announce(
                "Downloading packages that are to be deployed; IDs: "
                + string.Join(",", packagesToDeploy.Select(_ => _.Id)));
            var packagesWithTheirFiles =
                packagesToDeploy.Select(
                    _ =>
                    new Package
                        {
                            PackageDescription = _,
                            PackageFileBytes = this.packageManager.GetPackageFile(_),
                            PackageFileBytesRetrievalDateTimeUtc = DateTime.UtcNow
                        }).ToList();

            // get deployment details from Its.Config in the package
            var deploymentFileSearchPattern = string.Format(".config/{0}/Deployment.json", environment);

            this.announce("Searching for deployment configs in packages to be deployed.");
            var packagedDeploymentConfigs =
                packagesWithTheirFiles.Select(
                    _ =>
                    new PackagedDeploymentConfiguration()
                        {
                            Package = _,
                            DeploymentConfiguration =
                                Serializer.Deserialize<DeploymentConfiguration>(
                                    this.packageManager.GetFileContentsFromPackage(
                                        _,
                                        deploymentFileSearchPattern)),
                        }).ToList();

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

            // create new aws instance(s)
            this.announce("Creating new instance; Name: " + instanceName);
            var createdInstanceDescription = this.cloudManager.CreateNewInstance(instanceName, environment, configToCreateWith);

            this.announce(
                "Created new instance (waiting for Administrator password to be available); ID: "
                + createdInstanceDescription.Id + ", Private IP:" + createdInstanceDescription.PrivateIpAddress
                + ", Private DNS: " + createdInstanceDescription.PrivateDns);
            string adminPassword = null;
            var sleepTimeInSeconds = 30d;
            var privateKey = this.tracker.GetPrivateKeyOfInstanceById(createdInstanceDescription.Id);
            while (adminPassword == null)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                try
                {
                    adminPassword = this.cloudManager.GetAdministratorPasswordForInstance(createdInstanceDescription, privateKey);
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

            // this is necessary for finishing start up items, might have to try a few times until WinRM is available...
            this.announce("Rebooting new instance to finalize any items from user data setup.");
            sleepTimeInSeconds = 1d;
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

            var packagedConfigsWithFlatteningAndOverride =
                packagedConfigsWithDefaults.Select(
                    _ =>
                    new PackagedDeploymentConfiguration
                        {
                            DeploymentConfiguration = configToCreateWith,
                            Package = _.Package,
                        }).ToList();

            // get all web initializations to update any DNS entries on the public IP address.
            var webInitializations =
                packagedConfigsWithFlatteningAndOverride.SelectMany(
                    _ =>
                    _.DeploymentConfiguration.InitializationStrategies.Select(
                        strat => strat as InitializationStrategyWeb)).Where(_ => _ != null).ToList();

            foreach (var webInitialization in webInitializations)
            {
                this.cloudManager.UpsertDnsEntry(
                    createdInstanceDescription.Location,
                    webInitialization.PrimaryDns,
                    new[] { createdInstanceDescription.PublicIpAddress });
            }

            foreach (var packagedConfig in packagedConfigsWithFlatteningAndOverride)
            {
                var setupActions = packagedConfig.GetSetupSteps(this.certificateRetriever, environment);
                this.announce("Running setup actions for package ID: " + packagedConfig.Package.PackageDescription.Id);
                foreach (var setupAction in setupActions)
                {
                    setupAction.SetupAction(machineManager);
                }
             
                // Mark the instance as having the successfully deployed packages
                this.tracker.ProcessDeployedPackage(
                    createdInstanceDescription.Id,
                    packagedConfig.Package.PackageDescription);
                this.announce("Finished deployment.");
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
