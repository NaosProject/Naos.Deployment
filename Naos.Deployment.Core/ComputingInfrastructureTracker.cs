// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComputingInfrastructureTracker.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Naos.Deployment.Contract;

    using Newtonsoft.Json;

    /// <inheritdoc />
    public class ComputingInfrastructureTracker : ITrackComputingInfrastructure
    {
        private readonly object fileSync = new object();
        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputingInfrastructureTracker"/> class.
        /// </summary>
        /// <param name="filePath">Path to save all state to.</param>
        public ComputingInfrastructureTracker(string filePath)
        {
            this.filePath = filePath;
        }

        /// <inheritdoc />
        public ICollection<InstanceDescription> GetInstancesByDeployedPackages(ICollection<PackageDescription> packages)
        {
            lock (this.fileSync)
            {
                var theSafe = this.LoadStateFromDisk();

                var instancesThatHaveAnyOfTheProvidedPackages =
                    theSafe.Instances.Where(
                        _ =>
                        _.InstanceDescription.DeployedPackages.Intersect(
                            packages,
                            new PackageDescriptionIdOnlyEqualityComparer()).Any()).ToList();

                var ret = instancesThatHaveAnyOfTheProvidedPackages.Select(_ => _.InstanceDescription).ToList();
                return ret;
            }
        }

        /// <inheritdoc />
        public void ProcessInstanceTermination(string systemId)
        {
            lock (this.fileSync)
            {
                var theSafe = this.LoadStateFromDisk();

                var toDelete = theSafe.Instances.SingleOrDefault(_ => _.InstanceDescription.Id == systemId);
                if (toDelete != null)
                {
                    theSafe.Instances.Remove(toDelete);
                }

                this.SaveStateToDisk(theSafe);
            }
        }

        /// <inheritdoc />
        public void ProcessInstanceCreation(InstanceDescription instanceDescription)
        {
            lock (this.fileSync)
            {
                var theSafe = this.LoadStateFromDisk();

                var toUpdate =
                    theSafe.Instances.SingleOrDefault(
                        _ => _.InstanceDetails.PrivateIpAddress == instanceDescription.PrivateIpAddress);
                if (toUpdate == null)
                {
                    throw new NullReferenceException(
                        "Expected to find a tracked instance (pre-creation) with private IP: "
                        + instanceDescription.PrivateIpAddress);
                }

                toUpdate.InstanceDescription = instanceDescription;

                this.SaveStateToDisk(theSafe);
            }
        }

        /// <inheritdoc />
        public void ProcessDeployedPackage(string systemId, PackageDescription package)
        {
            lock (this.fileSync)
            {
                var theSafe = this.LoadStateFromDisk();

                var toUpdate =
                    theSafe.Instances.SingleOrDefault(
                        _ => _.InstanceDescription.Id == systemId);
                if (toUpdate == null)
                {
                    throw new NullReferenceException(
                        "Expected to find a tracked instance (post-creation) with system ID: "
                        + systemId);
                }

                toUpdate.InstanceDescription.DeployedPackages.Add(package);

                this.SaveStateToDisk(theSafe);
            }
        }

        /// <inheritdoc />
        public InstanceDescription GetInstanceDescriptionById(string systemId)
        {
            lock (this.fileSync)
            {
                var theSafe = this.LoadStateFromDisk();

                var wrapped = theSafe.Instances.FirstOrDefault(_ => _.InstanceDescription.Id == systemId);

                return wrapped == null ? null : wrapped.InstanceDescription;
            }
        }

        /// <inheritdoc />
        public string GetInstanceIdByName(string name)
        {
            lock (this.fileSync)
            {
                var theSafe = this.LoadStateFromDisk();

                var wrapped = theSafe.Instances.FirstOrDefault(_ => _.InstanceDescription.Name == name);

                return wrapped == null ? null : wrapped.InstanceDescription.Id;
            }
        }

        /// <inheritdoc />
        public string GetPrivateKeyOfInstanceById(string systemId)
        {
            lock (this.fileSync)
            {
                var theSafe = this.LoadStateFromDisk();

                var wrapped = theSafe.Instances.FirstOrDefault(_ => _.InstanceDescription.Id == systemId);

                if (wrapped == null)
                {
                    return null;
                }

                var containerId = wrapped.InstanceDetails.ContainerDetails.ContainerId;

                var container = theSafe.Containers.SingleOrDefault(_ => _.ContainerId == containerId);

                if (container == null)
                {
                    throw new NullReferenceException("Could not find Container: " + containerId);
                }

                return container.PrivateKey;
            }
        }

        /// <inheritdoc />
        public string GetDomainZoneId(string domain)
        {
            lock (this.fileSync)
            {
                var theSafe = this.LoadStateFromDisk();

                string ret = null;
                var found = theSafe.RootDomainHostingIdMap.TryGetValue(domain, out ret);
                return found ? ret : null;
            }
        }

        /// <inheritdoc />
        public InstanceDetails CreateInstanceDetails(DeploymentConfiguration deploymentConfig)
        {
            lock (this.fileSync)
            {
                var theSafe = this.LoadStateFromDisk();

                var ret = new InstanceDetails()
                              {
                                  DefaultDriveType = "gp2",
                                  ImageDetails =
                                      new ImageDetails()
                                          {
                                              OwnerAlias = "amazon",
                                              SearchPattern = theSafe.FindImageSearchPattern(deploymentConfig),
                                              ShouldHaveSingleMatch = false,
                                          },
                                  PrivateIpAddress = theSafe.FindIpAddress(deploymentConfig),
                                  KeyName = theSafe.FindKeyName(deploymentConfig),
                                  SecurityGroupId = theSafe.FindSecurityGroupId(deploymentConfig),
                                  Location = theSafe.FindLocation(deploymentConfig),
                                  ContainerDetails =
                                      new ContainerDetails()
                                          {
                                              ContainerId =
                                                  theSafe.FindContainerId(
                                                      deploymentConfig),
                                              ContainerLocation =
                                                  theSafe.FindContainerLocation(
                                                      deploymentConfig),
                                          },
                              };

                var newTracked = new InstanceWrapper()
                                     {
                                         InstanceDescription = new InstanceDescription()
                                                                   {
                                                                       Location = ret.Location,
                                                                       PrivateIpAddress = ret.PrivateIpAddress,
                                                                       DeployedPackages = new List<PackageDescription>(),
                                                                   },
                                         InstanceDetails = ret,
                                         DeploymentConfig = deploymentConfig,
                                     };

                theSafe.Instances.Add(newTracked);

                this.SaveStateToDisk(theSafe);
                return ret;
            }
        }

        private TheSafe LoadStateFromDisk()
        {
            if (!File.Exists(this.filePath))
            {
                this.SaveStateToDisk(new TheSafe());
            }

            var raw = File.ReadAllText(this.filePath);
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new KnownTypeConverter());
            var ret = JsonConvert.DeserializeObject<TheSafe>(raw, settings);
            if (ret.Instances == null)
            {
                ret.Instances = new List<InstanceWrapper>();
            }

            return ret;
        }

        private void SaveStateToDisk(TheSafe theSafe)
        {
            var serialized = JsonConvert.SerializeObject(theSafe);
            File.WriteAllText(this.filePath, serialized);
        }
    }

    /// <summary>
    /// Container object for storing instances in tracking.
    /// </summary>
    public class InstanceWrapper
    {
        /// <summary>
        /// Gets or sets the related instance description.
        /// </summary>
        public InstanceDescription InstanceDescription { get; set; }

        /// <summary>
        /// Gets or sets the related instance details.
        /// </summary>
        public InstanceDetails InstanceDetails { get; set; }

        /// <summary>
        /// Gets or sets the related deployment configuration.
        /// </summary>
        public DeploymentConfiguration DeploymentConfig { get; set; }
    }
}
