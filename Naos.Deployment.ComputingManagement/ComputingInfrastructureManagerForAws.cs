// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComputingInfrastructureManagerForAws.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.ComputingManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.AWS.Contract;
    using Naos.AWS.Core;
    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    using CheckState = Naos.Deployment.Domain.CheckState;
    using InstanceState = Naos.Deployment.Domain.InstanceState;
    using InstanceStatus = Naos.Deployment.Domain.InstanceStatus;

    /// <inheritdoc />
    public class ComputingInfrastructureManagerForAws : IManageComputingInfrastructure
    {
        private const string ElasticIpIdKeyForSystemSpecificDictionary = "elasticIpId";
        private const string AmiIdKeyForSystemSpecificDictionary = "amiId";
        private const string InstanceTypeKeyForSystemSpecificDictionary = "instanceType";

        private readonly ComputingInfrastructureManagerSettings settings;
        private readonly ITrackComputingInfrastructure tracker;

        private CredentialContainer credentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputingInfrastructureManagerForAws"/> class.
        /// </summary>
        /// <param name="settings">Settings for things like instance type maps, etc.</param>
        /// <param name="tracker">Tracking the resources manager.</param>
        public ComputingInfrastructureManagerForAws(ComputingInfrastructureManagerSettings settings, ITrackComputingInfrastructure tracker)
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
        public IManageComputingInfrastructure InitializeCredentials(
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
        /// <param name="location">Computing platform provider location to make the call against.</param>
        /// <param name="tokenLifespan">Life span of the credentials.</param>
        /// <param name="username">Username of the credentials.</param>
        /// <param name="password">Password of the credentials.</param>
        /// <param name="virtualMfaDeviceId">Virtual MFA device id of the credentials.</param>
        /// <param name="mfaValue">Token from the MFA device to use when authenticating.</param>
        /// <returns>New credentials for use in performing operations against the computing platform provider.</returns>
        public static CredentialContainer GetNewCredentials(
            string location,
            TimeSpan tokenLifespan,
            string username,
            string password,
            string virtualMfaDeviceId,
            string mfaValue)
        {
            var credentialManager = new CredentialManager();
            var task = Task.Run(() => credentialManager.GetSessionTokenCredentialsAsync(
                location,
                tokenLifespan,
                username,
                password,
                virtualMfaDeviceId,
                mfaValue));
            task.Wait();
            var credentialsToUse = task.Result;
            return credentialsToUse;
        }

        /// <summary>
        /// Initializes the credentials to credentials that have already been created.
        /// </summary>
        /// <param name="credentialsToUse">Valid credentials to use.</param>
        /// <returns>The class.</returns>
        public IManageComputingInfrastructure InitializeCredentials(CredentialContainer credentialsToUse)
        {
            this.credentials = credentialsToUse;

            return this;
        }

        /// <inheritdoc />
        public async Task TerminateInstanceAsync(string environment, string systemId, string systemLocation, bool releasePublicIpIfApplicable = false)
        {
            var instanceDescription = await this.tracker.GetInstanceDescriptionByIdAsync(environment, systemId);
            if (!string.IsNullOrEmpty(instanceDescription.PublicIpAddress))
            {
                // has a public IP that needs to be dissassociated and released before moving on...
                var elasticIp = new ElasticIp()
                                    {
                                        Id = instanceDescription.SystemSpecificDetails[ElasticIpIdKeyForSystemSpecificDictionary],
                                        PublicIpAddress = instanceDescription.PublicIpAddress,
                                        Region = systemLocation
                                    };
                await elasticIp.DisassociateFromInstanceAsync(this.credentials);
                if (releasePublicIpIfApplicable)
                {
                    await elasticIp.ReleaseAsync(this.credentials);
                }
            }

            var instanceToTerminate = new Instance() { Id = systemId, Region = systemLocation };
            await instanceToTerminate.TerminateAsync(this.credentials);
            await this.tracker.ProcessInstanceTerminationAsync(environment, instanceToTerminate.Id);
        }

        /// <inheritdoc />
        public async Task TurnOffInstanceAsync(string systemId, string systemLocation, bool waitUntilOff = true)
        {
            var instanceToTurnOff = new Instance() { Id = systemId, Region = systemLocation };
            await instanceToTurnOff.StopAsync(this.credentials);
            if (waitUntilOff)
            {
                await instanceToTurnOff.WaitForStateAsync(Naos.AWS.Contract.InstanceState.Stopped, this.credentials);
            }
        }

        /// <inheritdoc />
        public async Task TurnOnInstanceAsync(string systemId, string systemLocation, bool waitUntilOn = true)
        {
            var instanceToTurnOn = new Instance() { Id = systemId, Region = systemLocation };
            await instanceToTurnOn.StartAsync(this.credentials);
            if (waitUntilOn)
            {
                await instanceToTurnOn.WaitForStateAsync(Naos.AWS.Contract.InstanceState.Running, this.credentials);
                await instanceToTurnOn.WaitForSuccessfulChecksAsync(this.credentials);
            }
        }

        /// <inheritdoc />
        public async Task ChangeInstanceTypeAsync(string systemId, string systemLocation, InstanceType newInstanceType)
        {
            var newAwsInstanceType = this.GetAwsInstanceType(newInstanceType);
            var instanceToChangeTypeof = new Instance()
                                             {
                                                 Id = systemId,
                                                 Region = systemLocation,
                                                 InstanceType = newAwsInstanceType
                                             };

            await instanceToChangeTypeof.UpdateInstanceTypeAsync(this.credentials);
        }

        /// <inheritdoc />
        public async Task<InstanceStatus> GetInstanceStatusAsync(string systemId, string systemLocation)
        {
            var instance = new Instance { Id = systemId, Region = systemLocation };
            var awsStatus = await instance.GetStatusAsync(this.credentials);

            var state = (InstanceState)Enum.Parse(typeof(InstanceState), awsStatus.InstanceState.ToString(), true);

            var systemChecks = awsStatus.SystemChecks.ToDictionary(
                key => key.Key,
                value => (CheckState)Enum.Parse(typeof(CheckState), value.Value.ToString(), true));

            var instanceChecks = awsStatus.InstanceChecks.ToDictionary(
                key => key.Key,
                value => (CheckState)Enum.Parse(typeof(CheckState), value.Value.ToString(), true));

            var ret = new InstanceStatus { InstanceState = state, SystemChecks = systemChecks, InstanceChecks = instanceChecks };
            return ret;
        }

        /// <inheritdoc />
        public async Task<InstanceDescription> CreateNewInstanceAsync(string environment, string name, DeploymentConfiguration deploymentConfiguration, ICollection<PackageDescription> intendedPackages, bool includeInstanceInitializationScript)
        {
            var existing = await this.tracker.GetInstanceIdByNameAsync(environment, name);
            if (existing != null)
            {
                throw new DeploymentException(
                    "Instance trying to be created with name: " + name
                    + " but an instance with this name already exists; ID: " + existing);
            }

            var instanceDetails = await this.tracker.GetNewInstanceCreationDetailsAsync(environment, deploymentConfiguration, intendedPackages);

            var namer = new ComputingInfrastructureNamer(
                name,
                environment,
                instanceDetails.ComputingContainerDescription.ContainerLocation);

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
                                                                      Id = instanceDetails.ComputingContainerDescription.ContainerId,
                                                                      AvailabilityZone = instanceDetails.ComputingContainerDescription.ContainerLocation, 
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
                                       includeInstanceInitializationScript
                                           ? this.settings.GetInstanceCreationUserData()
                                           : string.Empty
                               };

            var createdInstance = await instanceToCreate.CreateAsync(userData, this.credentials);

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

            await createdInstance.AddTagInAwsAsync(this.settings.EnvironmentTagKey, environment, this.credentials);

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

            await this.tracker.ProcessInstanceCreationAsync(instanceDescription);

            return instanceDescription;
        }

        /// <inheritdoc />
        public async Task<InstanceDescription> GetInstanceDescriptionAsync(string environment, string name)
        {
            var id = await this.tracker.GetInstanceIdByNameAsync(environment, name);
            var ret = await this.tracker.GetInstanceDescriptionByIdAsync(environment, id);
            return ret;
        }

        /// <inheritdoc />
        public async Task<IList<InstanceDetailsFromComputingPlatform>> GetActiveInstancesFromProviderAsync(string environment, string systemLocation)
        {
            var instances = await new List<InstanceWithStatus>().FillFromAwsAsync(systemLocation, this.credentials);

            var ret =
                instances.Where(_ => _.InstanceStatus.InstanceState != Naos.AWS.Contract.InstanceState.Terminated).Select(
                    _ =>
                        {
                            string name;
                            var tags = _.Tags ?? new Dictionary<string, string>();
                            var found = tags.TryGetValue(this.settings.NameTagKey, out name);
                            if (!found)
                            {
                                name = "UNNAMED-" + Guid.NewGuid().ToString().ToUpper();
                            }

                            var status = new InstanceStatus
                                             {
                                                 InstanceState = (InstanceState)Enum.Parse(typeof(InstanceState), _.InstanceStatus.InstanceState.ToString(), true),
                                                 SystemChecks = new Dictionary<string, CheckState>(),
                                                 InstanceChecks = new Dictionary<string, CheckState>()
                                             };

                            return new InstanceDetailsFromComputingPlatform
                                       {
                                           Id = _.Id,
                                           Location = _.Region,
                                           Name = name,
                                           Tags = tags,
                                           InstanceStatus = status,
                                           PrivateIpAddress = _.PrivateIpAddress
                                       };
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
        public async Task<string> GetAdministratorPasswordForInstanceAsync(InstanceDescription instanceDescription, string privateKey)
        {
            var instanceToGetPasswordFor = new Instance()
                                               {
                                                   Id = instanceDescription.Id,
                                                   Region = instanceDescription.Location,
                                                   Key = new KeyPair() { PrivateKey = privateKey }
                                               };

            string password;
            try
            {
                password = await instanceToGetPasswordFor.GetAdministratorPasswordAsync(this.credentials);
            }
            catch (NullPasswordDataException ex)
            {
                throw new PasswordUnavailableException(
                    instanceDescription.Id,
                    "Password was unavailable, please try again later.",
                    ex);
            }

            return password;
        }

        /// <inheritdoc />
        public async Task UpsertDnsEntryAsync(string environment, string location, string domain, ICollection<string> ipAddresses)
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

            var hostingId = await this.tracker.GetDomainZoneIdAsync(environment, rootDomain);
            var dnsManager = new Route53Manager(this.credentials);
            await dnsManager.UpsertDnsEntryAsync(location, hostingId, Route53EntryType.A, domain, ipAddresses);
        }
    }
}