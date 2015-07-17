// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SingleFileCloudInfrastructureTrackerContainer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CloudInfrastructureTracking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Contract;

    /// <summary>
    /// File to keep all state for ComputingInfrastructureTracker.
    /// </summary>
    public class SingleFileCloudInfrastructureTrackerContainer
    {
        /// <summary>
        /// Finds the correct IP address based on configuration.
        /// </summary>
        /// <param name="environment">The environment being deployed to.</param>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>IP Address to use to create an instance.</returns>
        public string FindIpAddress(string environment, DeploymentConfiguration deploymentConfig)
        {
            var container = this.GetContainer(environment, deploymentConfig);
            for (int idx = container.StartIpsAfter + 1; idx < 256; idx++)
            {
                var sampleIp = container.Cidr.Replace("0/24", idx.ToString());
                if (this.Instances.All(_ => _.InstanceCreationDetails.PrivateIpAddress != sampleIp))
                {
                    return sampleIp;
                }
            }

            throw new DeploymentException("Can't find an IPAddress that isn't taken");
        }

        /// <summary>
        /// Find the encryption key name.
        /// </summary>
        /// <param name="environment">The environment being deployed to.</param>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Encryption key name.</returns>
        public string FindKeyName(string environment, DeploymentConfiguration deploymentConfig)
        {
            return this.GetContainer(environment, deploymentConfig).KeyName;
        }

        /// <summary>
        /// Find the security group ID to use.
        /// </summary>
        /// <param name="environment">The environment being deployed to.</param>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Security group ID to use.</returns>
        public string FindSecurityGroupId(string environment, DeploymentConfiguration deploymentConfig)
        {
            return this.GetContainer(environment, deploymentConfig).SecurityGroupId;
        }

        /// <summary>
        /// Find the location to use.
        /// </summary>
        /// <param name="environment">The environment being deployed to.</param>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Location to use.</returns>
        public string FindLocation(string environment, DeploymentConfiguration deploymentConfig)
        {
            throw new NotImplementedException("Removed for replacement system, will remove entire single file system when finished.");
        }

        /// <summary>
        /// Find the container ID to use.
        /// </summary>
        /// <param name="environment">The environment being deployed to.</param>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Container ID to use.</returns>
        public string FindContainerId(string environment, DeploymentConfiguration deploymentConfig)
        {
            return this.GetContainer(environment, deploymentConfig).ContainerId;
        }

        /// <summary>
        /// Find the container location to use.
        /// </summary>
        /// <param name="environment">The environment being deployed to.</param>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Container location to use.</returns>
        public string FindContainerLocation(string environment, DeploymentConfiguration deploymentConfig)
        {
            return this.GetContainer(environment, deploymentConfig).ContainerLocation;
        }

        private CloudContainerDescription GetContainer(string environment, DeploymentConfiguration deploymentConfig)
        {
            ICollection<CloudContainerDescription> outCollection;
            var found = this.EnvironmentCloudContainerMap.TryGetValue(environment, out outCollection);
            if (!found)
            {
                return null;
            }

            return outCollection.Single(_ => _.InstanceAccessibility == deploymentConfig.InstanceAccessibility);
        }

        /// <summary>
        /// Find the image search pattern to use.
        /// </summary>
        /// <param name="environment">The environment being deployed to.</param>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Image search pattern to use.</returns>
        public string FindImageSearchPattern(string environment, DeploymentConfiguration deploymentConfig)
        {
            string searchPattern;
            var success = this.WindowsSkuSearchPatternMap.TryGetValue(deploymentConfig.InstanceType.WindowsSku, out searchPattern);
            if (!success)
            {
                throw new DeploymentException("Unsupported Windows SKU: " + deploymentConfig.InstanceType.WindowsSku);
            }

            return searchPattern;
        }

        /// <summary>
        /// Gets or sets the configured container ID.
        /// </summary>
        public IDictionary<string, ICollection<CloudContainerDescription>> EnvironmentCloudContainerMap { get; set; }

        /// <summary>
        /// Gets or sets a map of root domains and their hosting ID.
        /// </summary>
        public IDictionary<string, string> RootDomainHostingIdMap { get; set; } 

        /// <summary>
        /// Gets or sets the a map of configured search patterns to Windows SKU's.
        /// </summary>
        public IDictionary<WindowsSku, string> WindowsSkuSearchPatternMap { get; set; }

        /// <summary>
        /// Gets or sets the wrapped instances.
        /// </summary>
        public List<InstanceWrapper> Instances { get; set; }

        /// <summary>
        /// Gets or sets the instance private DNS root domain to use (e.g. machines.my-company.com).
        /// </summary>
        public string InstancePrivateDnsRootDomain { get; set; }
    }
}