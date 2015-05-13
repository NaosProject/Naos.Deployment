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

    using Naos.Deployment.Contract;
    using Naos.WinRM;

    using Newtonsoft.Json;

    /// <inheritdoc />
    public class DeploymentManager : IManageDeployments
    {
        private readonly ITrackComputingInfrastructure tracker;

        private readonly IManageCloudInfrastructure cloudManager;

        private readonly IManagePackages packageManager;

        private readonly DeploymentConfiguration defaultDeploymentConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentManager"/> class.
        /// </summary>
        /// <param name="tracker">Tracker of computing infrastructure.</param>
        /// <param name="cloudManager">Manager of the cloud infrastructure (wraps custom cloud interactions).</param>
        /// <param name="packageManager">Proxy to retrieve packages.</param>
        /// <param name="defaultDeploymentConfig">Default deployment configuration to substitute the values for any nulls.</param>
        public DeploymentManager(
            ITrackComputingInfrastructure tracker,
            IManageCloudInfrastructure cloudManager,
            IManagePackages packageManager,
            DeploymentConfiguration defaultDeploymentConfig)
        {
            this.tracker = tracker;
            this.cloudManager = cloudManager;
            this.packageManager = packageManager;
            this.defaultDeploymentConfig = defaultDeploymentConfig;
        }

        /// <inheritdoc />
        public void DeployPackages(
            ICollection<PackageDescription> packagesToDeploy,
            string environment,
            string instanceName,
            DeploymentConfiguration deploymentConfigOverride = null)
        {
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = string.Join("--", packagesToDeploy.Select(_ => _.Id).ToArray());
            }

            // get aws instance object by name (from the AWS object tracking storage)
            var instances = this.tracker.GetInstancesByDeployedPackages(packagesToDeploy);

            // confirm that terminating the instances will not take down any packages that aren't getting re-deployed...
            var deployedPackages = instances.SelectMany(_ => _.DeployedPackages).ToList();
            if (deployedPackages.Except(packagesToDeploy).Any())
            {
                var deployedIdList = string.Join(",", deployedPackages.Select(_ => _.Id));
                var deployingIdList = string.Join(",", packagesToDeploy.Select(_ => _.Id));
                throw new DeploymentException(
                    "Cannot proceed because taking down the instances of requested packages will take down packages not getting redeployed; Running: "
                    + deployedIdList + " Deploying: " + deployingIdList);
            }

            // terminate instance(s) if necessary (if it exists)
            foreach (var instanceDescription in instances)
            {
                this.cloudManager.Terminate(instanceDescription.Id, instanceDescription.Location);
            }

            // get the NuGet package to crack open for Its.Config deployment file
            var deploymentFileSearchPattern = string.Format(".config/{0}/Deployment.json", environment);
            var fileContents =
                packagesToDeploy.Select(_ => this.packageManager.GetFileContentsFromPackage(_, deploymentFileSearchPattern))
                    .ToList();

            // get deployment details from Its.Config in the package
            var deploymentConfigs =
                fileContents.Where(_ => _ != null)
                    .Select(JsonConvert.DeserializeObject<DeploymentConfiguration>).ToList();

            // apply default values to any nulls
            var appliedDefaults = deploymentConfigs.Count == 0
                                      ? new[] { this.defaultDeploymentConfig }.ToList()
                                      : deploymentConfigs.Select(_ => _.ApplyDefaults(this.defaultDeploymentConfig)).ToList();

            // flatten configs into a single config to deploy onto an instance
            var flattenedConfig = appliedDefaults.Flatten();

            // apply overrides
            var overriddenConfig = flattenedConfig.ApplyOverrides(deploymentConfigOverride);

            // create new aws instance(s)
            var createdInstanceDescription = this.cloudManager.Create(instanceName, overriddenConfig);

            string adminPassword = null;
            var sleepTimeInSeconds = .25;
            var privateKey = this.tracker.GetPrivateKeyOfInstanceById(createdInstanceDescription.Id);
            while (adminPassword == null)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 2;
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));
                adminPassword = this.cloudManager.GetAdministratorPasswordForInstance(createdInstanceDescription, privateKey);
            }

            // finalize instance creation (WinRM reboot, etc.)
            var machineManager = new MachineManager(
                createdInstanceDescription.PrivateIpAddress,
                "Administrator",
                MachineManager.ConvertStringToSecureString(adminPassword));

            // this is necessary for finishing start up items...
            machineManager.Reboot();

            // TODO: use the initialization strategies to build a chain of actions w/ the machineManager to execute...

            // configure necessary software
            // machineManager.InstallCertificate();

            // DONT forget to process packages as they are installed...
            this.tracker.ProcessDeployedPackage(createdInstanceDescription.Id, packagesToDeploy.First());
        }

        /// <summary>
        /// Gets the password for the instance.
        /// </summary>
        /// <param name="cloudManager">CloudManager to use.</param>
        /// <param name="tracker">Tracker of computing infrastructure.</param>
        /// <param name="instanceToSearchFor">Instance to find the password for.</param>
        /// <returns>Password for instance.</returns>
        public string GetPassword(IManageCloudInfrastructure cloudManager, ITrackComputingInfrastructure tracker, InstanceDescription instanceToSearchFor)
        {
            string adminPassword = null;
            var sleepTimeInSeconds = .25;
            var privateKey = tracker.GetPrivateKeyOfInstanceById(instanceToSearchFor.Id);
            while (adminPassword == null)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 2;
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));
                adminPassword = cloudManager.GetAdministratorPasswordForInstance(instanceToSearchFor, privateKey);
            }

            return adminPassword;
        }
    }
}
