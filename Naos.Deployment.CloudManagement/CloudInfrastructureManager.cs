// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloudInfrastructureManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.CloudManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting.Messaging;

    using Naos.AWS.Contract;
    using Naos.AWS.Core;
    using Naos.Deployment.Contract;

    /// <inheritdoc />
    public class CloudInfrastructureManager : IManageCloudInfrastructure
    {
        private const string ElasticIpIdKeyForSystemSpecificDictionary = "elasticIpId";
        private const string AmiIdKeyForSystemSpecificDictionary = "amiId";
        private const string InstanceTypeKeyForSystemSpecificDictionary = "instanceType";

        private readonly CloudInfrastructureManagerSettings settings;
        private readonly ITrackComputingInfrastructure tracker;

        private CredentialContainer credentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudInfrastructureManager"/> class.
        /// </summary>
        /// <param name="settings">Settings for things like instance type maps, etc.</param>
        /// <param name="tracker">Tracking the resources manager.</param>
        public CloudInfrastructureManager(CloudInfrastructureManagerSettings settings, ITrackComputingInfrastructure tracker)
        {
            this.settings = settings;
            this.tracker = tracker;
        }

        /// <summary>
        /// Initialize the manager with credential information necessary to get credentials created, returns itself so it can be chained from the constructor.
        /// </summary>
        /// <param name="location">Region to authenticate against.</param>
        /// <param name="tokenLifespan">Lifetime to allow the single use security token to be 'alive' for.</param>
        /// <param name="username">The username/access key to use to authenticate.</param>
        /// <param name="password">The password/secret key to use to authenticate.</param>
        /// <param name="virtualMfaDeviceId">The identifier of the software based MFA token provider to use.</param>
        /// <param name="mfaValue">The one time value of the software based MFA token provider.</param>
        /// <returns>The class.</returns>
        public IManageCloudInfrastructure InitializeCredentials(
            string location,
            TimeSpan tokenLifespan,
            string username,
            string password,
            string virtualMfaDeviceId,
            string mfaValue)
        {
            var credentialsToUse = GetNewCredentials(
                location,
                tokenLifespan,
                username,
                password,
                virtualMfaDeviceId,
                mfaValue);

            return this.InitializeCredentials(credentialsToUse);
        }

        /// <summary>
        /// Get a new set of credentials using provided information.
        /// </summary>
        /// <param name="location">Cloud provider location to make the call against.</param>
        /// <param name="tokenLifespan">Life span of the credentials.</param>
        /// <param name="username">Username of the credentials.</param>
        /// <param name="password">Password of the credentials.</param>
        /// <param name="virtualMfaDeviceId">Virtual MFA device id of the credentials.</param>
        /// <param name="mfaValue">Token from the MFA device to use when authenticating.</param>
        /// <returns>New credentials for use in performing operations against the cloud provider.</returns>
        public static CredentialContainer GetNewCredentials(
            string location,
            TimeSpan tokenLifespan,
            string username,
            string password,
            string virtualMfaDeviceId,
            string mfaValue)
        {
            var credentialManager = new CredentialManager();
            var credentialsToUse = credentialManager.GetSessionTokenCredentials(
                location,
                tokenLifespan,
                username,
                password,
                virtualMfaDeviceId,
                mfaValue);
            return credentialsToUse;
        }

        /// <summary>
        /// Initializes the credentials to credentials that have already been created.
        /// </summary>
        /// <param name="credentialsToUse">Valid credentials to use.</param>
        /// <returns>The class.</returns>
        public IManageCloudInfrastructure InitializeCredentials(CredentialContainer credentialsToUse)
        {
            this.credentials = credentialsToUse;

            return this;
        }

        /// <inheritdoc />
        public void TerminateInstance(string environment, string systemId, string systemLocation, bool releasePublicIpIfApplicable = false)
        {
            var instanceDescription = this.tracker.GetInstanceDescriptionById(environment, systemId);
            if (!string.IsNullOrEmpty(instanceDescription.PublicIpAddress))
            {
                // has a public IP that needs to be dissassociated and released before moving on...
                var elasticIp = new ElasticIp()
                                    {
                                        Id = instanceDescription.SystemSpecificDetails[ElasticIpIdKeyForSystemSpecificDictionary],
                                        PublicIpAddress = instanceDescription.PublicIpAddress,
                                        Region = systemLocation
                                    };
                elasticIp.DisassociateFromInstance(this.credentials);
                if (releasePublicIpIfApplicable)
                {
                    elasticIp.Release(this.credentials);
                }
            }

            var instanceToTerminate = new Instance() { Id = systemId, Region = systemLocation };
            instanceToTerminate.Terminate(this.credentials);
            this.tracker.ProcessInstanceTermination(environment, instanceToTerminate.Id);
        }

        /// <inheritdoc />
        public void TurnOffInstance(string systemId, string systemLocation, bool waitUntilOff = true)
        {
            var instanceToTurnOff = new Instance() { Id = systemId, Region = systemLocation };
            instanceToTurnOff.Stop(this.credentials);
            if (waitUntilOff)
            {
                instanceToTurnOff.WaitForState(InstanceState.Stopped, this.credentials);
            }
        }

        /// <inheritdoc />
        public void TurnOnInstance(string systemId, string systemLocation, bool waitUntilOn = true)
        {
            var instanceToTurnOn = new Instance() { Id = systemId, Region = systemLocation };
            instanceToTurnOn.Start(this.credentials);
            if (waitUntilOn)
            {
                instanceToTurnOn.WaitForState(InstanceState.Running, this.credentials);
            }
        }

        /// <inheritdoc />
        public void ChangeInstanceType(string systemId, string systemLocation, InstanceType newInstanceType)
        {
            var newAwsInstanceType = this.GetAwsInstanceType(newInstanceType);
            var instanceToChangeTypeof = new Instance()
                                             {
                                                 Id = systemId,
                                                 Region = systemLocation,
                                                 InstanceType = newAwsInstanceType
                                             };

            instanceToChangeTypeof.UpdateInstanceType(this.credentials);
        }

        /// <inheritdoc />
        public InstanceDescription CreateNewInstance(string environment, string name, DeploymentConfiguration deploymentConfiguration, ICollection<PackageDescription> intendedPackages, bool includeInstanceInializtionScript)
        {
            var instanceDetails = this.tracker.GetNewInstanceCreationDetails(environment, deploymentConfiguration, intendedPackages);

            var namer = new CloudInfrastructureNamer(
                name,
                environment,
                instanceDetails.CloudContainerDescription.ContainerLocation);

            Func<string, string> getDeviceNameFromDriveLetter = delegate(string driveLetter)
                {
                    string mapResult;
                    var foundResult = this.settings.DriveLetterVolumeDescriptorMap.TryGetValue(driveLetter, out mapResult);
                    if (!foundResult)
                    {
                        throw new DeploymentException("Drive letter not supported: " + driveLetter);
                    }

                    return mapResult;
                };

            Func<VolumeType, string> getVolumeTypeValueFromEnum = delegate(VolumeType volumeType)
                {
                    string mapResult;
                    var foundResult = this.settings.VolumeTypeValueMap.TryGetValue(volumeType, out mapResult);
                    if (!foundResult)
                    {
                        throw new DeploymentException("Volume type not supported: " + volumeType);
                    }

                    return mapResult;
                };

            var mappedVolumes =
                deploymentConfiguration.Volumes.Select(
                    _ =>
                    new EbsVolume()
                        {
                            Region = instanceDetails.Location,
                            Name = namer.GetVolumeName(_.DriveLetter),
                            SizeInGb = _.SizeInGb,
                            DeviceName = getDeviceNameFromDriveLetter(_.DriveLetter),
                            VolumeType = getVolumeTypeValueFromEnum(_.Type),
                            VirtualName = _.DriveLetter
                        }).ToList();

            var awsInstanceType = this.GetAwsInstanceType(deploymentConfiguration.InstanceType);

            var instanceName = namer.GetInstanceName();
            var existing = this.tracker.GetInstanceIdByName(environment, instanceName);
            if (existing != null)
            {
                throw new DeploymentException(
                    "Instance trying to be created with name: " + instanceName
                    + " but an instance with this name already exists; ID: " + existing);
            }

            var ami = new Ami()
                          {
                              Region = instanceDetails.Location,
                          };

            // if we don't have a specific image then create the search strategy otherwise just use specific one...
            if (string.IsNullOrEmpty(instanceDetails.ImageDetails.ImageSystemId))
            {
                var imageStrategy = new AmiSearchStrategy()
                {
                    OwnerAlias = instanceDetails.ImageDetails.OwnerAlias,
                    SearchPattern = instanceDetails.ImageDetails.SearchPattern,
                    MultipleFoundBehavior =
                        instanceDetails.ImageDetails.ShouldHaveSingleMatch
                            ? MultipleAmiFoundBehavior.Throw
                            : MultipleAmiFoundBehavior.FirstSortedDescending,
                };

                ami.SearchStrategy = imageStrategy;
            }
            else
            {
                ami.Id = instanceDetails.ImageDetails.ImageSystemId;
            }

            var instanceToCreate = new Instance()
                                       {
                                           Name = instanceName,
                                           Ami =
                                               ami,
                                           ContainingSubnet = new Subnet()
                                                                  {
                                                                      Id = instanceDetails.CloudContainerDescription.ContainerId,
                                                                      AvailabilityZone = instanceDetails.CloudContainerDescription.ContainerLocation, 
                                                                  },
                                           Key = new KeyPair() { KeyName = instanceDetails.KeyName },
                                           PrivateIpAddress = instanceDetails.PrivateIpAddress,
                                           SecurityGroup = new SecurityGroup() { Id = instanceDetails.SecurityGroupId },
                                           InstanceType = awsInstanceType,
                                           DisableApiTermination = false,
                                           MappedVolumes = mappedVolumes,
                                           Region = instanceDetails.Location,
                                           EnableSourceDestinationCheck = true,
                                       };

            if (deploymentConfiguration.InstanceAccessibility == InstanceAccessibility.Public)
            {
                instanceToCreate.ElasticIp = new ElasticIp
                                                 {
                                                     Name = instanceName + "-ElasticIp",
                                                     Region = instanceDetails.Location,
                                                 };
            }

            var userData = new UserData
                               {
                                   Data =
                                       includeInstanceInializtionScript
                                           ? this.settings.GetInstanceCreationUserData()
                                           : string.Empty
                               };

            var createdInstance = instanceToCreate.Create(userData, this.credentials);

            var systemSpecificDetails = new Dictionary<string, string>
                                            {
                                                {
                                                    AmiIdKeyForSystemSpecificDictionary,
                                                    createdInstance.Ami.Id
                                                },
                                                {
                                                    InstanceTypeKeyForSystemSpecificDictionary,
                                                    awsInstanceType
                                                }
                                            };

            if (createdInstance.ElasticIp != null)
            {
                systemSpecificDetails.Add(ElasticIpIdKeyForSystemSpecificDictionary, createdInstance.ElasticIp.Id);
            }

            var deployedPackages = intendedPackages.ToDictionary(
                item => item.Id,
                _ =>
                new PackageDescriptionWithDeploymentStatus
                    {
                        Id = _.Id,
                        Version = _.Version,
                        DeploymentStatus = PackageDeploymentStatus.NotYetDeployed
                    });

            createdInstance.AddTagInAws(Constants.EnvironmentTagKey, environment, this.credentials);

            var instanceDescription = new InstanceDescription()
            {
                Id = createdInstance.Id,
                Name = instanceName,
                ComputerName = name,
                Environment = environment,
                Location = createdInstance.Region,
                PublicIpAddress =
                    createdInstance.ElasticIp == null
                        ? null
                        : createdInstance.ElasticIp.PublicIpAddress,
                PrivateIpAddress = createdInstance.PrivateIpAddress,
                DeployedPackages = deployedPackages,
                SystemSpecificDetails = systemSpecificDetails,
            };

            this.tracker.ProcessInstanceCreation(instanceDescription);

            return instanceDescription;
        }

        /// <inheritdoc />
        public InstanceDescription GetInstanceDescription(string environment, string name)
        {
            var id = this.tracker.GetInstanceIdByName(environment, name);
            var ret = this.tracker.GetInstanceDescriptionById(environment, id);
            return ret;
        }

        /// <inheritdoc />
        public IList<InstanceDetailsFromCloud> GetInstancesFromCloud(string environment, string systemLocation)
        {
            var instances = new List<Instance>().FillFromAws(systemLocation, this.credentials);

            var ret =
                instances.Select(
                    _ =>
                    new InstanceDetailsFromCloud
                        {
                            Id = _.Id,
                            Location = _.Region,
                            Name = _.Tags[Constants.NameTagKey],
                            Tags = _.Tags
                        }).ToList();

            return ret;
        }

        /// <summary>
        /// Gets the AWS specific instance type from a generic InstanceType.
        /// </summary>
        /// <param name="instanceType">Instance type to use as basis.</param>
        /// <returns>AWS specific instance type that best matches the provided instance type.</returns>
        public string GetAwsInstanceType(InstanceType instanceType)
        {
            // if we don't have a specific instance type then look one up, otherwise use the specific one...
            if (!string.IsNullOrEmpty(instanceType.SpecificInstanceTypeSystemId))
            {
                return instanceType.SpecificInstanceTypeSystemId;
            }

            if (instanceType.WindowsSku == WindowsSku.SqlWeb)
            {
                foreach (var type in this.settings.AwsInstanceTypesForSqlWeb)
                {
                    if (type.VirtualCores >= instanceType.VirtualCores && type.RamInGb >= instanceType.RamInGb)
                    {
                        return type.AwsInstanceTypeDescriptor;
                    }
                }
            } 
            else if (instanceType.WindowsSku == WindowsSku.SqlStandard)
            {
                foreach (var type in this.settings.AwsInstanceTypesForSqlStandard)
                {
                    if (type.VirtualCores >= instanceType.VirtualCores && type.RamInGb >= instanceType.RamInGb)
                    {
                        return type.AwsInstanceTypeDescriptor;
                    }
                }
            }
            else
            {
                foreach (var type in this.settings.AwsInstanceTypes)
                {
                    if (type.VirtualCores >= instanceType.VirtualCores && type.RamInGb >= instanceType.RamInGb)
                    {
                        return type.AwsInstanceTypeDescriptor;
                    }
                }
            }

            throw new DeploymentException(
                "Could not find an AWS instance type that could support the specified needs; VirtualCores: "
                + instanceType.VirtualCores + ", RamInGb: " + instanceType.RamInGb);
        }

        /// <inheritdoc />
        public string GetAdministratorPasswordForInstance(InstanceDescription instanceDescription, string privateKey)
        {
            var instanceToGetPasswordFor = new Instance()
                                               {
                                                   Id = instanceDescription.Id,
                                                   Region = instanceDescription.Location,
                                                   Key = new KeyPair() { PrivateKey = privateKey }
                                               };

            var password = instanceToGetPasswordFor.GetAdministratorPassword(this.credentials);
            return password;
        }

        /// <inheritdoc />
        public void UpsertDnsEntry(string environment, string location, string domain, ICollection<string> ipAddresses)
        {
            // from: http://stackoverflow.com/questions/16473838/get-domain-name-of-a-url-in-c-sharp-net
            var host = new Uri("http://" + domain).Host;
            int index = host.LastIndexOf('.'), last = 3;
            while (index > 0 && index >= last - 3)
            {
                last = index;
                index = host.LastIndexOf('.', last - 1);
            }

            // need root domain to lookup zone id
            var rootDomain = host.Substring(index + 1); 

            var hostingId = this.tracker.GetDomainZoneId(environment, rootDomain);
            var dnsManager = new Route53Manager(this.credentials);
            dnsManager.UpsertDnsEntry(location, hostingId, Route53EntryType.A, domain, ipAddresses);
        }
    }
}