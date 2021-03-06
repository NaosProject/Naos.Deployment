// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Arcology.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    using OBeautifulCode.Assertion.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Container object for storing a single environments entire state.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arcology", Justification = "Spelling/name is correct.")]
    public class Arcology : ArcologyInfo
    {
        private readonly List<DeployedInstance> instances;

        /// <summary>
        /// Initializes a new instance of the <see cref="Arcology"/> class.
        /// </summary>
        /// <param name="environment">Environment of the arcology.</param>
        /// <param name="arcologyInfo">Information about the arcology.</param>
        /// <param name="deployedInstances">Optional deployed instances in the arcology.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "arcology", Justification = "Spelling/name is correct.")]
        public Arcology(string environment, ArcologyInfo arcologyInfo, IReadOnlyCollection<DeployedInstance> deployedInstances = null)
        {
            new { arcologyInfo }.AsArg().Must().NotBeNull();

            this.Environment = environment;
            this.SerializedEnvironmentSpecification = arcologyInfo.SerializedEnvironmentSpecification;
            this.ComputingContainers = arcologyInfo.ComputingContainers;
            this.RootDomainHostingIdMap = arcologyInfo.RootDomainHostingIdMap;
            this.Location = arcologyInfo.Location;
            this.WindowsSkuSearchPatternMap = arcologyInfo.WindowsSkuSearchPatternMap;
            this.instances = deployedInstances?.ToList() ?? new List<DeployedInstance>();
        }

        /// <summary>
        /// Gets the wrapped instances.
        /// </summary>
        public IReadOnlyCollection<DeployedInstance> Instances => this.instances;

        /// <summary>
        /// Updates the internal instance list to remove an instance.
        /// </summary>
        /// <param name="instanceToRemove">Instance to remove from collection.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "arcology", Justification = "Spelling/name is correct.")]
        public void MutateInstancesRemove(DeployedInstance instanceToRemove)
        {
            new { instanceToRemove }.AsArg().Must().NotBeNull();

            var removed = this.instances.Remove(instanceToRemove);
            if (!removed)
            {
                throw new DeploymentException("Failed to removed instance from arcology; ID: " + instanceToRemove.InstanceDescription.Id);
            }
        }

        /// <summary>
        /// Updates the internal instance list to add an instance.
        /// </summary>
        /// <param name="instanceToAdd">Instance to add to the collection.</param>
        public void MutateInstancesAdd(DeployedInstance instanceToAdd)
        {
            this.instances.Add(instanceToAdd);
        }

        /// <summary>
        /// Gets or sets the name of the environment.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets the list of instances that have the specified packages deployed (in any state) to them.
        /// </summary>
        /// <param name="packages">Package list to use for finding instances.</param>
        /// <returns>The list of instances that have the specified packages deployed (in any state) to them.</returns>
        public IReadOnlyCollection<InstanceDescription> GetInstancesByDeployedPackages(IReadOnlyCollection<PackageDescription> packages)
        {
            var instancesThatHaveAnyOfTheProvidedPackages =
                this.Instances.Where(
                    _ =>
                    _.InstanceDescription.DeployedPackages.Values.Select(p => p.PackageDescription).Intersect(
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
            var found = this.RootDomainHostingIdMap.TryGetValue(domain, out string ret);
            return found ? ret : null;
        }

        /// <summary>
        /// Gets the system location.
        /// </summary>
        /// <returns>System location.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Want a method.")]
        public string GetSystemLocation()
        {
            var ret = this.Location;
            return ret;
        }

        /// <summary>
        /// Looks up the key used for the instance and returns the private key.
        /// </summary>
        /// <param name="systemId">ID from the computing platform provider of the instance.</param>
        /// <returns>Private key of instance.</returns>
        public string GetPrivateKeyOfInstanceById(string systemId)
        {
            var wrapped = this.Instances.FirstOrDefault(_ => _.InstanceDescription.Id == systemId);

            if (wrapped == null)
            {
                return null;
            }

            var containerId = wrapped.InstanceCreationDetails.ComputingContainerDescription.ContainerId;

            var container = this.ComputingContainers.SingleOrDefault(_ => _.ContainerId == containerId);

            if (container == null)
            {
                throw new DeploymentException("Could not find Container: " + containerId);
            }

            var decryptedKey = Encryptor.Decrypt(container.EncryptedPrivateKey, container.EncryptingCertificateLocator);
            return decryptedKey;
        }

        /// <summary>
        /// Get private key for computing container of specific accessibility.
        /// </summary>
        /// <param name="accessibility">Accessibility to get the private key for.</param>
        /// <returns>Private key of computing container.</returns>
        public string GetPrivateKeyOfComputingContainer(InstanceAccessibility accessibility)
        {
            var container = this.ComputingContainers.SingleOrDefault(_ => _.InstanceAccessibility == accessibility);

            if (container == null)
            {
                throw new DeploymentException(Invariant($"Could not find Container with accessibility {accessibility}"));
            }

            var decryptedKey = Encryptor.Decrypt(container.EncryptedPrivateKey, container.EncryptingCertificateLocator);
            return decryptedKey;
        }

        /// <summary>
        /// Gets the instance description by ID.
        /// </summary>
        /// <param name="systemId">ID from the computing platform provider of the instance.</param>
        /// <returns>InstanceDescription if any by that ID.</returns>
        public InstanceDescription GetInstanceDescriptionById(string systemId)
        {
            var wrapped = this.Instances.FirstOrDefault(_ => _.InstanceDescription.Id == systemId);

            return wrapped?.InstanceDescription;
        }

        /// <summary>
        /// Gets the instance ID by name.
        /// </summary>
        /// <param name="name">Name of the instance (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <returns>ID of instance by name if found.</returns>
        public string GetInstanceIdByName(string name)
        {
            // use the computer name because that is the short name, x.Name will be full cloud name...
            var wrapped =
                this.Instances.FirstOrDefault(
                    _ => string.Equals(_.InstanceDescription.ComputerName, name, StringComparison.CurrentCultureIgnoreCase));

            return wrapped?.InstanceDescription.Id;
        }

        /// <summary>
        /// Gets instance details necessary to track and create an instance.
        /// </summary>
        /// <param name="deploymentConfiguration">Deployment requirements.</param>
        /// <param name="intendedPackages">Packages that are planned to be deployed.</param>
        /// <returns>Object holding information necessary to track and create an instance.</returns>
        public DeployedInstance CreateNewDeployedInstance(DeploymentConfiguration deploymentConfiguration, IReadOnlyCollection<PackageDescriptionWithOverrides> intendedPackages)
        {
            new { deploymentConfiguration }.AsArg().Must().NotBeNull();

            var privateIpAddress = this.FindIpAddress(deploymentConfiguration);
            var location = this.Location;
            var container = this.GetComputingContainer(deploymentConfiguration);
            var containerId = container.ContainerId;
            var containerLocation = container.ContainerLocation;
            var securityGroupId = container.SecurityGroupId;
            var keyName = container.KeyName;

            ImageDetails imageDetails;
            var windowsOs = deploymentConfiguration.InstanceType.OperatingSystem as OperatingSystemDescriptionWindows;
            var linuxOs = deploymentConfiguration.InstanceType.OperatingSystem as OperatingSystemDescriptionLinux;
            if ((windowsOs?.Sku == WindowsSku.SpecificImageSupplied) || (linuxOs?.Distribution == LinuxDistribution.SpecificImageSupplied))
            {
                if (string.IsNullOrEmpty(deploymentConfiguration.InstanceType.SpecificImageSystemId))
                {
                    throw new ArgumentException(Invariant($"Cannot specify an operating system of {deploymentConfiguration.InstanceType.OperatingSystem} without specifying a {nameof(deploymentConfiguration)}.{nameof(deploymentConfiguration.InstanceType)}.{nameof(deploymentConfiguration.InstanceType.SpecificImageSystemId)}"));
                }

                imageDetails = new ImageDetails()
                                   {
                                       ImageSystemId = deploymentConfiguration.InstanceType.SpecificImageSystemId,
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
                ImageDetails =
                    imageDetails,
                PrivateIpAddress = privateIpAddress,
                KeyName = keyName,
                SecurityGroupId = securityGroupId,
                Location = location,
                ComputingContainerDescription =
                    new ComputingContainerDescription()
                    {
                        ContainerId = containerId,
                        ContainerLocation = containerLocation,
                    },
            };

            var deployedPackages = intendedPackages.ToDictionary(
                item => item.PackageDescription.Id,
                _ =>
                new PackageDescriptionWithDeploymentStatus
                    {
                        PackageDescription = _.PackageDescription,
                        DeploymentStatus = PackageDeploymentStatus.NotYetDeployed,
                        InitializationStrategies = _.InitializationStrategies,
                        ItsConfigOverrides = _.ItsConfigOverrides,
                    });

            var newTracked = new DeployedInstance()
                                 {
                                     InstanceDescription =
                                         new InstanceDescription()
                                             {
                                                 Environment = this.Environment,
                                                 Location = ret.Location,
                                                 PrivateIpAddress = ret.PrivateIpAddress,
                                                 DeployedPackages = deployedPackages,
                                             },
                                     InstanceCreationDetails = ret,
                                     DeploymentConfig = deploymentConfiguration,
                                 };

            return newTracked;
        }

        /// <summary>
        /// Updates the deployed packages list for the specified instance.
        /// </summary>
        /// <param name="instanceToUpdatePackagesOn">Instance to update the package list on.</param>
        /// <param name="package">Package that was successfully deployed.</param>
        public static void UpdatePackageVerificationInInstanceDeploymentList(DeployedInstance instanceToUpdatePackagesOn, PackageDescription package)
        {
            new { instanceToUpdatePackagesOn }.AsArg().Must().NotBeNull();

            PackageDescriptionIdOnlyEqualityComparer comparer = new PackageDescriptionIdOnlyEqualityComparer();
            var existing =
                instanceToUpdatePackagesOn.InstanceDescription.DeployedPackages.Where(_ => comparer.Equals(_.Value.PackageDescription, package)).ToList();
            if (existing.Any())
            {
                var existingSingle = existing.Single().Key;
                instanceToUpdatePackagesOn.InstanceDescription.DeployedPackages[existingSingle].DeploymentStatus =
                    PackageDeploymentStatus.DeployedSuccessfully;
            }
            else
            {
                var toAdd = new PackageDescriptionWithDeploymentStatus
                                {
                                    PackageDescription = package,
                                    DeploymentStatus =
                                        PackageDeploymentStatus
                                        .DeployedSuccessfully,
                                    InitializationStrategies = new InitializationStrategyBase[0],
                                    ItsConfigOverrides = new ItsConfigOverride[0],
                                };

                instanceToUpdatePackagesOn.InstanceDescription.DeployedPackages.Add(package.Id, toAdd);
            }
        }

        private string FindImageSearchPattern(DeploymentConfiguration deploymentConfig)
        {
            if (!(deploymentConfig.InstanceType.OperatingSystem is OperatingSystemDescriptionWindows windowsOs))
            {
                throw new DeploymentException("Unsupported Operating System to find image by pattern search: " + deploymentConfig.InstanceType.OperatingSystem);
            }

            var success = this.WindowsSkuSearchPatternMap.TryGetValue(windowsOs.Sku, out string searchPattern);
            if (!success)
            {
                throw new DeploymentException("Unsupported Windows SKU: " + windowsOs.Sku);
            }

            return searchPattern;
        }

        private ComputingContainerDescription GetComputingContainer(DeploymentConfiguration deploymentConfig)
        {
            return this.ComputingContainers.Single(_ => _.InstanceAccessibility == deploymentConfig.InstanceAccessibility);
        }

        private string FindIpAddress(DeploymentConfiguration deploymentConfig)
        {
            var container = this.GetComputingContainer(deploymentConfig);
            for (int idx = container.StartIpsAfter + 1; idx < 256; idx++)
            {
                var sampleIp = container.Cidr.Replace("0/24", idx.ToString(CultureInfo.CurrentCulture));
                if (this.Instances.All(_ => _.InstanceCreationDetails.PrivateIpAddress != sampleIp))
                {
                    return sampleIp;
                }
            }

            throw new DeploymentException("Can't find an IP Address that isn't taken");
        }
    }
}
