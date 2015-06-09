// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloudInfrastructureManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.AWS.Contract;
    using Naos.AWS.Core;
    using Naos.Deployment.Contract;

    /// <inheritdoc />
    public class CloudInfrastructureManager : IManageCloudInfrastructure
    {
        private const string ElasticIpIdKeyForSystemSpecificDictionary = "elasticIpId";
        private const string AmiIdKeyForSystemSpecificDictionary = "amiId";

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
        public void Terminate(string systemId, string systemLocation, bool releasePublicIpIfApplicable = false)
        {
            var instanceDescription = this.tracker.GetInstanceDescriptionById(systemId);
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
            this.tracker.ProcessInstanceTermination(instanceToTerminate.Id);
        }

        /// <inheritdoc />
        public InstanceDescription CreateNewInstance(string name, string environment, DeploymentConfiguration deploymentConfiguration)
        {
            var instanceDetails = this.tracker.CreateInstanceDetails(deploymentConfiguration);
            var privateDnsRootDomain = this.tracker.GetInstancePrivateDnsRootDomain();

            var imageStrategy = new AmiSearchStrategy()
                                    {
                                        OwnerAlias = instanceDetails.ImageDetails.OwnerAlias,
                                        SearchPattern = instanceDetails.ImageDetails.SearchPattern,
                                        MultipleFoundBehavior =
                                            instanceDetails.ImageDetails.ShouldHaveSingleMatch
                                                ? MultipleAmiFoundBehavior.Throw
                                                : MultipleAmiFoundBehavior.FirstSortedDescending,
                                    };

            var namer = new CloudInfrastructureNamer(
                name,
                environment,
                instanceDetails.ContainerDetails.ContainerLocation,
                privateDnsRootDomain);

            Func<string, string> getDeviceNameFromDriveLetter = delegate(string driveLetter)
                {
                    string mapResult;
                    var foundResult = this.settings.DriveLetterVolumeDescriptorMap.TryGetValue(driveLetter, out mapResult);
                    if (!foundResult)
                    {
                        throw new NotSupportedException("Drive letter not supported: " + driveLetter);
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
                            VolumeType = instanceDetails.DefaultDriveType,
                            VirtualName = _.DriveLetter
                        }).ToList();

            var awsInstanceType = this.GetAwsInstanceType(deploymentConfiguration.InstanceType);
            var instancePrivateDns = namer.GetPrivateDns();
            var instanceName = namer.GetInstanceName();
            var existing = this.tracker.GetInstanceIdByName(instanceName);
            if (existing != null)
            {
                throw new DeploymentException(
                    "Instance trying to be created with name: " + instanceName
                    + " but an instance with this name already exists; ID: " + existing);
            }

            var instanceToCreate = new Instance()
                                       {
                                           Name = instanceName,
                                           Ami =
                                               new Ami()
                                                   {
                                                       Region = instanceDetails.Location,
                                                       SearchStrategy = imageStrategy
                                                   },
                                           ContainingSubnet = new Subnet()
                                                                  {
                                                                      Id = instanceDetails.ContainerDetails.ContainerId,
                                                                      AvailabilityZone = instanceDetails.ContainerDetails.ContainerLocation, 
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

            var userData = new UserData() { Data = this.settings.GetInstanceCreationUserData(name) };

            var createdInstance = instanceToCreate.Create(userData, this.credentials);

            var systemSpecificDetails = new Dictionary<string, string>
                                            {
                                                {
                                                    AmiIdKeyForSystemSpecificDictionary,
                                                    createdInstance.Ami.Id
                                                }
                                            };

            if (createdInstance.ElasticIp != null)
            {
                systemSpecificDetails.Add(ElasticIpIdKeyForSystemSpecificDictionary, createdInstance.ElasticIp.Id);
            }

            // add private dns entry for the machine.
            this.UpsertDnsEntry(
                createdInstance.Region,
                instancePrivateDns,
                new[] { createdInstance.PrivateIpAddress });

            var instanceDescription = new InstanceDescription()
            {
                Id = createdInstance.Id,
                Name = instanceName,
                Environment = environment,
                Location = createdInstance.Region,
                PublicIpAddress =
                    createdInstance.ElasticIp == null
                        ? null
                        : createdInstance.ElasticIp.PublicIpAddress,
                PrivateIpAddress = createdInstance.PrivateIpAddress,
                DeployedPackages = new List<PackageDescription>(),
                PrivateDns = instancePrivateDns,
                SystemSpecificDetails = systemSpecificDetails,
            };

            this.tracker.ProcessInstanceCreation(instanceDescription);

            return instanceDescription;
        }

        /// <inheritdoc />
        public InstanceDescription GetInstanceDescription(string name)
        {
            var id = this.tracker.GetInstanceIdByName(name);
            var ret = this.tracker.GetInstanceDescriptionById(id);
            return ret;
        }

        /// <summary>
        /// Gets the AWS specific instance type from a generic InstanceType.
        /// </summary>
        /// <param name="instanceType">Instance type to use as basis.</param>
        /// <returns>AWS specific instance type that best matches the provided instance type.</returns>
        public string GetAwsInstanceType(InstanceType instanceType)
        {
            foreach (var type in this.settings.AwsInstanceTypes)
            {
                if (type.VirtualCores >= instanceType.VirtualCores && type.RamInGb >= instanceType.RamInGb)
                {
                    return type.AwsInstanceTypeDescriptor;
                }
            }

            throw new NotSupportedException(
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
        public void UpsertDnsEntry(string location, string domain, ICollection<string> ipAddresses)
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

            var hostingId = this.tracker.GetDomainZoneId(rootDomain);
            var dnsManager = new Route53Manager(this.credentials);
            dnsManager.UpsertDnsEntry(location, hostingId, Route53EntryType.A, domain, ipAddresses);
        }
    }
}