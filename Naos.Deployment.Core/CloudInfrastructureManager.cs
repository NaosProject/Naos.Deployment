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
        private const string ElasticIpIdKeyForSystemSpecificDictionary = "ElasticIpId";
        private readonly ITrackComputingInfrastructure tracker;

        private CredentialContainer credentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudInfrastructureManager"/> class.
        /// </summary>
        /// <param name="tracker">Tracking the resources manager.</param>
        public CloudInfrastructureManager(ITrackComputingInfrastructure tracker)
        {
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
            var credentialManager = new CredentialManager();
            var credentialsToUse = credentialManager.GetSessionTokenCredentials(
                location,
                tokenLifespan,
                username,
                password,
                virtualMfaDeviceId,
                mfaValue);

            return this.InitializeCredentials(credentialsToUse);
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

            var imageStrategy = new AmiSearchStrategy()
                                    {
                                        OwnerAlias = instanceDetails.ImageDetails.OwnerAlias,
                                        SearchPattern = instanceDetails.ImageDetails.SearchPattern,
                                        MultipleFoundBehavior =
                                            instanceDetails.ImageDetails.ShouldHaveSingleMatch
                                                ? MultipleAmiFoundBehavior.Throw
                                                : MultipleAmiFoundBehavior.FirstSortedDescending,
                                    };

            var namer = new CloudInfrastructureNamer(name, instanceDetails.ContainerDetails.ContainerLocation);

            Func<string, string> getDeviceNameFromDriveLetter = delegate(string driveLetter)
                {
                    switch (driveLetter)
                    {
                        case "C":
                            return "/dev/sda1";
                        case "D":
                            return "xvdb";
                        case "E":
                            return "xvdc";
                        case "F":
                            return "xvdd";
                        default:
                            throw new NotSupportedException("Drive letter not supported: " + driveLetter);
                    }
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

            var awsInstanceType = GetAwsInstanceType(deploymentConfiguration.InstanceType);
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

            var userData = new UserData() { Data = this.GetUserData(name) };

            var createdInstance = instanceToCreate.Create(userData, this.credentials);

            var systemSpecificDetails = new Dictionary<string, string>();
            if (createdInstance.ElasticIp != null)
            {
                systemSpecificDetails.Add(ElasticIpIdKeyForSystemSpecificDictionary, createdInstance.ElasticIp.Id);
            }

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
        public static string GetAwsInstanceType(InstanceType instanceType)
        {
            var theDatasheet = new[]
                                   {
                                       new { AwsType = "t1.micro", RamInGb = 0.613, VirtualCores = 1 },
                                       new { AwsType = "t2.medium", RamInGb = 4.0, VirtualCores = 2 },
                                       new { AwsType = "t2.micro", RamInGb = 1.0, VirtualCores = 1 },
                                       new { AwsType = "t2.small", RamInGb = 2.0, VirtualCores = 1 },
                                       new { AwsType = "m1.small", RamInGb = 1.7, VirtualCores = 1 },
                                       new { AwsType = "m1.medium", RamInGb = 3.75, VirtualCores = 1 },
                                       new { AwsType = "m1.large", RamInGb = 7.5, VirtualCores = 2 },
                                       new { AwsType = "m1.xlarge", RamInGb = 15.0, VirtualCores = 4 },
                                       new { AwsType = "m2.xlarge", RamInGb = 17.1, VirtualCores = 2 },
                                       new { AwsType = "m2.2xlarge", RamInGb = 34.2, VirtualCores = 4 },
                                       new { AwsType = "m2.4xlarge", RamInGb = 68.4, VirtualCores = 8 },
                                       new { AwsType = "m3.large", RamInGb = 7.5, VirtualCores = 2 },
                                       new { AwsType = "m3.medium", RamInGb = 3.75, VirtualCores = 1 },
                                       new { AwsType = "m3.xlarge", RamInGb = 15.0, VirtualCores = 4 },
                                       new { AwsType = "m3.2xlarge", RamInGb = 30.0, VirtualCores = 8 },
                                       new { AwsType = "c1.medium", RamInGb = 1.7, VirtualCores = 2 },
                                       new { AwsType = "c1.xlarge", RamInGb = 7.0, VirtualCores = 8 },
                                       new { AwsType = "c3.large", RamInGb = 3.75, VirtualCores = 2 },
                                       new { AwsType = "c3.xlarge", RamInGb = 7.5, VirtualCores = 4 },
                                       new { AwsType = "c3.2xlarge", RamInGb = 15.0, VirtualCores = 8 },
                                       new { AwsType = "c3.4xlarge", RamInGb = 30.0, VirtualCores = 16 },
                                       new { AwsType = "c3.8xlarge", RamInGb = 60.0, VirtualCores = 32 },
                                       new { AwsType = "c4.large", RamInGb = 3.75, VirtualCores = 2 },
                                       new { AwsType = "c4.xlarge", RamInGb = 7.5, VirtualCores = 4 },
                                       new { AwsType = "c4.2xlarge", RamInGb = 15.0, VirtualCores = 8 },
                                       new { AwsType = "c4.4xlarge", RamInGb = 30.0, VirtualCores = 16 },
                                       new { AwsType = "c4.8xlarge", RamInGb = 60.0, VirtualCores = 36 },
                                       new { AwsType = "g2.2xlarge", RamInGb = 15.0, VirtualCores = 8 },
                                       new { AwsType = "r3.large", RamInGb = 15.25, VirtualCores = 2 },
                                       new { AwsType = "r3.xlarge", RamInGb = 30.5, VirtualCores = 4 },
                                       new { AwsType = "r3.2xlarge", RamInGb = 61.0, VirtualCores = 8 },
                                       new { AwsType = "r3.4xlarge", RamInGb = 122.0, VirtualCores = 16 },
                                       new { AwsType = "r3.8xlarge", RamInGb = 244.0, VirtualCores = 32 },
                                       new { AwsType = "i2.xlarge", RamInGb = 30.5, VirtualCores = 4 },
                                       new { AwsType = "i2.2xlarge", RamInGb = 61.0, VirtualCores = 8 },
                                       new { AwsType = "i2.4xlarge", RamInGb = 122.0, VirtualCores = 16 },
                                       new { AwsType = "i2.8xlarge", RamInGb = 244.0, VirtualCores = 32 },
                                       new { AwsType = "d2.xlarge", RamInGb = 30.5, VirtualCores = 4 },
                                       new { AwsType = "d2.2xlarge", RamInGb = 61.0, VirtualCores = 8 },
                                       new { AwsType = "d2.4xlarge", RamInGb = 122.0, VirtualCores = 16 },
                                       new { AwsType = "d2.8xlarge", RamInGb = 244.0, VirtualCores = 36 },
                                   };

            foreach (var type in theDatasheet)
            {
                if (type.VirtualCores >= instanceType.VirtualCores && type.RamInGb >= instanceType.RamInGb)
                {
                    return type.AwsType;
                }
            }

            throw new NotSupportedException(
                "Could not find an AWS instance type that could support the specified needs; VirtualCores: "
                + instanceType.VirtualCores + ", RamInGb: " + instanceType.RamInGb);
        }

        private string GetUserData(string name)
        {
            return @"
<powershell>
# BLOCK NAME: powershellBlock-enableRemoting
winrm quickconfig -q
winrm set winrm/config/winrs '@{MaxMemoryPerShellMB=""300""}'
winrm set winrm/config '@{MaxTimeoutms=""1800000""}'
netsh advfirewall firewall add rule name=""WinRM 5985"" protocol=TCP dir=in localport=5985 action=allow
netsh advfirewall firewall add rule name=""WinRM 5986"" protocol=TCP dir=in localport=5986 action=allow
net stop winrm
sc.exe config winrm start=auto
net start winrm

# BLOCK NAME: powershellBlock-enableScriptExecution
Set-ExecutionPolicy 'Unrestricted' -Force

# ADDING COMMANDS TO CONFIGURE WINDOWS UPDATE TO RUN EVERYDAY AT 3AM AND INSTALL IMPORTANT UPDATES AUTOMATICALLY
$windowsUpdateSettings = (New-Object -com 'Microsoft.Update.AutoUpdate').Settings
$windowsUpdateSettings.NotificationLevel = 4
$windowsUpdateSettings.Save()

# ADDING COMMANDS TO CONFIGURE TIME TO UPDATE AUTOMATICALLY (on by default but must restart the service)
NET STOP W32Time
NET START W32Time

# ADDING COMMAND TO INSTALL CHOCOLATEY FOR APPLICATION INSTALLS LATER
iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))

# ADDING RENAME COMMAND BECAUSE 'computerName' WAS PRESENT IN CONFIG
Rename-Computer -NewName '" + name + @"' -Force
</powershell>
                    ";
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