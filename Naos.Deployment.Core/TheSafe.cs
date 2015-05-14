// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TheSafe.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Contract;

    /// <summary>
    /// File to keep all state for ComputingInfrastructureTracker.
    /// </summary>
    public class TheSafe
    {
        /// <summary>
        /// Finds the correct IP address based on configuration.
        /// </summary>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>IP Address to use to create an instance.</returns>
        public string FindIpAddress(DeploymentConfiguration deploymentConfig)
        {
            var container = this.GetContainer(deploymentConfig);
            for (int idx = container.StartIpsAfter + 1; idx < 256; idx++)
            {
                var sampleIp = container.Cidr.Replace("0/24", idx.ToString());
                if (this.Instances.All(_ => _.InstanceDetails.PrivateIpAddress != sampleIp))
                {
                    return sampleIp;
                }
            }

            throw new NotSupportedException("Can't find an IPAddress that isn't taken");
        }

        /// <summary>
        /// Find the encryption key name.
        /// </summary>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Encryption key name.</returns>
        public string FindKeyName(DeploymentConfiguration deploymentConfig)
        {
            return this.GetContainer(deploymentConfig).KeyName;
        }

        /// <summary>
        /// Find the security group ID to use.
        /// </summary>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Security group ID to use.</returns>
        public string FindSecurityGroupId(DeploymentConfiguration deploymentConfig)
        {
            return this.GetContainer(deploymentConfig).SecurityGroupId;
        }

        /// <summary>
        /// Find the location to use.
        /// </summary>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Location to use.</returns>
        public string FindLocation(DeploymentConfiguration deploymentConfig)
        {
            return this.GetContainer(deploymentConfig).Location;
        }

        /// <summary>
        /// Find the container ID to use.
        /// </summary>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Container ID to use.</returns>
        public string FindContainerId(DeploymentConfiguration deploymentConfig)
        {
            return this.GetContainer(deploymentConfig).ContainerId;
        }

        /// <summary>
        /// Find the container location to use.
        /// </summary>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Container location to use.</returns>
        public string FindContainerLocation(DeploymentConfiguration deploymentConfig)
        {
            return this.GetContainer(deploymentConfig).ContainerLocation;
        }

        private ContainerDetails GetContainer(DeploymentConfiguration deploymentConfig)
        {
            return this.Containers.Single(_ => _.InstanceAccessibility == deploymentConfig.InstanceAccessibility);
        }

        /// <summary>
        /// Find the image search pattern to use.
        /// </summary>
        /// <param name="deploymentConfig">Deployment configuration in question.</param>
        /// <returns>Image search pattern to use.</returns>
        public string FindImageSearchPattern(DeploymentConfiguration deploymentConfig)
        {
            return this.DefaultInstanceImageSearchPattern;
        }

        /// <summary>
        /// Gets or sets the configured container ID.
        /// </summary>
        public ICollection<ContainerDetails> Containers { get; set; }

        /// <summary>
        /// Gets or sets the configured search pattern.
        /// </summary>
        public string DefaultInstanceImageSearchPattern { get; set; }

        /// <summary>
        /// Gets or sets the wrapped instances.
        /// </summary>
        public List<InstanceWrapper> Instances { get; set; }
    }
}