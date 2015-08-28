// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Arcology.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CloudInfrastructureTracking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Amazon.SimpleDB.Model;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Container object for storing a single environments entire state.
    /// </summary>
    internal class Arcology
    {
        /// <summary>
        /// Gets or sets a map of root domains and their hosting ID.
        /// </summary>
        public IDictionary<string, string> RootDomainHostingIdMap { get; set; }

        /// <summary>
        /// Gets or sets the wrapped instances.
        /// </summary>
        public List<InstanceWrapper> Instances { get; set; }

        /// <summary>
        /// Gets or sets the name of the environment.
        /// </summary>
        public string Environment { get; set; }

        public ICollection<InstanceDescription> GetInstancesByDeployedPackages(ICollection<PackageDescription> packages)
        {
            var instancesThatHaveAnyOfTheProvidedPackages =
                this.Instances.Where(
                    _ =>
                    _.InstanceDescription.DeployedPackages.Values.Intersect(
                        packages,
                        new PackageDescriptionIdOnlyEqualityComparer()).Any()).ToList();

            var ret = instancesThatHaveAnyOfTheProvidedPackages.Select(_ => _.InstanceDescription).ToList();
            return ret;
        }

        /// <summary>
        /// Looks up the hosting ID of the specified ROOT domain (null if not found).
        /// </summary>
        /// <param name="domain">Domain to find the hosting ID for (should only be a root domain).</param>
        /// <returns>Hosting ID or null if not found.</returns>
        public string GetDomainZoneId(string domain)
        {
            string ret;
            var found = this.RootDomainHostingIdMap.TryGetValue(domain, out ret);
            return found ? ret : null;
        }

        /// <summary>
        /// Gets or sets the a map of configured search patterns to Windows SKU's.
        /// </summary>
        public IDictionary<WindowsSku, string> WindowsSkuSearchPatternMap { get; set; }

        /// <summary>
        /// Gets or sets the configured container ID.
        /// </summary>
        public ICollection<CloudContainerDescription> CloudContainers { get; set; }

        /// <summary>
        /// Gets or sets the cloud location of arcology.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Looks up the key used for the instance and returns the private key.
        /// </summary>
        /// <param name="systemId">ID from the cloud provider of the instance.</param>
        /// <returns>Private key of instance.</returns>
        public string GetPrivateKeyOfInstanceById(string systemId)
        {
            var wrapped = this.Instances.FirstOrDefault(_ => _.InstanceDescription.Id == systemId);

            if (wrapped == null)
            {
                return null;
            }

            var containerId = wrapped.InstanceCreationDetails.CloudContainerDescription.ContainerId;

            var container = this.CloudContainers.SingleOrDefault(_ => _.ContainerId == containerId);

            if (container == null)
            {
                throw new DeploymentException("Could not find Container: " + containerId);
            }

            return container.PrivateKey;
        }

        /// <summary>
        /// Gets the instance description by ID.
        /// </summary>
        /// <param name="systemId">ID from the cloud provider of the instance.</param>
        /// <returns>InstanceDescription if any by that ID.</returns>
        public InstanceDescription GetInstanceDescriptionById(string systemId)
        {
            var wrapped = this.Instances.FirstOrDefault(_ => _.InstanceDescription.Id == systemId);

            return wrapped == null ? null : wrapped.InstanceDescription;
        }

        /// <summary>
        /// Gets the instance ID by name.
        /// </summary>
        /// <param name="name">Name of the instance.</param>
        /// <returns>ID of instance by name if found.</returns>
        public string GetInstanceIdByName(string name)
        {
            var wrapped =
                this.Instances.FirstOrDefault(
                    _ => string.Equals(_.InstanceDescription.Name, name, StringComparison.CurrentCultureIgnoreCase));

            return wrapped == null ? null : wrapped.InstanceDescription.Id;
        }

        /// <summary>
        /// Gets instance details necessary to hand off to the cloud provider.
        /// </summary>
        /// <param name="deploymentConfiguration">Deployment requirements.</param>
        /// <param name="intendedPackages">Packages that are planned to be deployed.</param>
        /// <returns>Object holding information necessary to create an instance.</returns>
        public InstanceCreationDetails MakeNewInstanceCreationDetails(DeploymentConfiguration deploymentConfiguration, ICollection<PackageDescription> intendedPackages)
        {
            var privateIpAddress = this.FindIpAddress(deploymentConfiguration);
            var location = this.Location;
            var cloudContainer = this.GetCloudContainer(deploymentConfiguration);
            var containerId = cloudContainer.ContainerId;
            var containerLocation = cloudContainer.ContainerLocation;
            var securityGroupId = cloudContainer.SecurityGroupId;
            var keyName = cloudContainer.KeyName;

            ImageDetails imageDetails;
            if (deploymentConfiguration.InstanceType.WindowsSku == WindowsSku.SpecificImageSupplied)
            {
                if (string.IsNullOrEmpty(deploymentConfiguration.InstanceType.SpecificImageSystemId))
                {
                    throw new ArgumentException("Cannot specify deploymentConfiguration.InstanceType.WindowsSku.SpecificImageSupplied without specifying a deploymentConfiguration.InstanceType.SpecificImageSystemId");
                }

                imageDetails = new ImageDetails()
                                   {
                                       ImageSystemId = deploymentConfiguration.InstanceType.SpecificImageSystemId
                                   };
            }
            else
            {
                var amiSearchPattern = this.FindImageSearchPattern(deploymentConfiguration);
                imageDetails = new ImageDetails()
                {
                    OwnerAlias = "amazon",
                    SearchPattern = amiSearchPattern,
                    ShouldHaveSingleMatch = false,
                };
            }

            var ret = new InstanceCreationDetails()
            {
                DefaultDriveType = "gp2",
                ImageDetails =
                    imageDetails,
                PrivateIpAddress = privateIpAddress,
                KeyName = keyName,
                SecurityGroupId = securityGroupId,
                Location = location,
                CloudContainerDescription =
                    new CloudContainerDescription()
                    {
                        ContainerId = containerId,
                        ContainerLocation = containerLocation,
                    },
            };

            var deployedPackages = intendedPackages.ToDictionary(
                item => item.Id,
                _ =>
                new PackageDescriptionWithDeploymentStatus
                    {
                        Id = _.Id,
                        Version = _.Version,
                        DeploymentStatus = PackageDeploymentStatus.NotYetDeployed
                    });

            var newTracked = new InstanceWrapper()
                                 {
                                     InstanceDescription =
                                         new InstanceDescription()
                                             {
                                                 Location = ret.Location,
                                                 PrivateIpAddress = ret.PrivateIpAddress,
                                                 DeployedPackages = deployedPackages,
                                             },
                                     InstanceCreationDetails = ret,
                                     DeploymentConfig = deploymentConfiguration,
                                 };

            this.Instances.Add(newTracked);
            return ret;
        }

        /// <summary>
        /// Updates instance information that is available post creation by binding on IP Address.
        /// </summary>
        /// <param name="instanceDescription">Description of the created instance.</param>
        public void UpdateInstanceDescription(InstanceDescription instanceDescription)
        {
            var toUpdate =
                this.Instances.SingleOrDefault(
                    _ => _.InstanceCreationDetails.PrivateIpAddress == instanceDescription.PrivateIpAddress);

            if (toUpdate == null)
            {
                throw new DeploymentException(
                    "Expected to find a tracked instance (pre-creation) with private IP: "
                    + instanceDescription.PrivateIpAddress);
            }

            toUpdate.InstanceDescription = instanceDescription;
        }

        /// <summary>
        /// Updates the deployed packages list for the specified instance.
        /// </summary>
        /// <param name="systemId">ID from the cloud provider of the instance.</param>
        /// <param name="package">Package that was successfully deployed.</param>
        public void UpdatePackageVerificationInInstanceDeploymentList(string systemId, PackageDescription package)
        {
            var toUpdate = this.Instances.SingleOrDefault(_ => _.InstanceDescription.Id == systemId);

            if (toUpdate == null)
            {
                throw new DeploymentException(
                    "Expected to find a tracked instance (post-creation) with system ID: "
                    + systemId);
            }

            PackageDescriptionIdOnlyEqualityComparer comparer = new PackageDescriptionIdOnlyEqualityComparer();
            var existing =
                toUpdate.InstanceDescription.DeployedPackages.Where(_ => comparer.Equals(_.Value, package)).ToList();
            if (existing.Any())
            {
                var existingSingle = existing.Single().Key;
                toUpdate.InstanceDescription.DeployedPackages[existingSingle].DeploymentStatus =
                    PackageDeploymentStatus.DeployedSuccessfully;
            }
            else
            {
                var toAdd = new PackageDescriptionWithDeploymentStatus
                                {
                                    Id = package.Id,
                                    Version = package.Version,
                                    DeploymentStatus =
                                        PackageDeploymentStatus
                                        .DeployedSuccessfully
                                };

                toUpdate.InstanceDescription.DeployedPackages.Add(package.Id, toAdd);
            }
        }

        private string FindImageSearchPattern(DeploymentConfiguration deploymentConfig)
        {
            string searchPattern;
            var success = this.WindowsSkuSearchPatternMap.TryGetValue(deploymentConfig.InstanceType.WindowsSku, out searchPattern);
            if (!success)
            {
                throw new DeploymentException("Unsupported Windows SKU: " + deploymentConfig.InstanceType.WindowsSku);
            }

            return searchPattern;
        }

        private CloudContainerDescription GetCloudContainer(DeploymentConfiguration deploymentConfig)
        {
            return this.CloudContainers.Single(_ => _.InstanceAccessibility == deploymentConfig.InstanceAccessibility);
        }
    
        private string FindIpAddress(DeploymentConfiguration deploymentConfig)
        {
            var container = this.GetCloudContainer(deploymentConfig);
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
    }
}