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
        /// Gets the largest instance type of the list of provided types.
        /// </summary>
        /// <param name="instanceTypes">List of instance types to filter to largest.</param>
        /// <returns>The largest instance type of the provided list.</returns>
        public static string InferLargestInstanceType(ICollection<string> instanceTypes)
        {
            // there's probably a better algorithm for this, it doesn't run very often and it's what i've got... -Lawson 2015/05/01
            if (instanceTypes == null)
            {
                throw new ArgumentException("Cannot infer the largest instance type from a null list or a list with nulls.");
            }

            if (!instanceTypes.Any())
            {
                throw new ArgumentException("Cannot infer the largest instance type from an empty list.");
            }

            if (instanceTypes.Count == 1)
            {
                return instanceTypes.Single();
            }

            var wrappedInstanceTypes = instanceTypes.Select(_ => new { SearchString = _, InstanceType = _ }).ToList();

            // prefixes in order from smallest to largest
            var prefixes = new[] { "t", "m", "c", "g", "r", "i", "d" };

            // if this isn't one then the later filter will need to be updated!!
            var prefixLength = 1;

            // suffixes in order from smallest to largest
            var suffixes = new[] { "micro", "small", "medium", "large", "xlarge", "2xlarge", "4xlarge", "8xlarge" };

            // post suffix numbers from smallest to largest
            var postPrefixNumbers = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            Func<dynamic, dynamic> nullObjectTrimmer = item => item;
            Func<dynamic, dynamic> prefixTrimmer =
                (item) =>
                new
                    {
                        SearchString = item.SearchString.Substring(prefixLength, item.SearchString.Length - prefixLength),
                        InstanceType = item.InstanceType
                    };
            Func<dynamic, dynamic> justAfterDotTrimmer =
                (item) => new { SearchString = item.SearchString.Split('.')[1], InstanceType = item.InstanceType };

            var theMatrix = new[]
                                {
                                    new { Trimmer = nullObjectTrimmer, BucketPrefixes = prefixes, },
                                    new { Trimmer = prefixTrimmer, BucketPrefixes = postPrefixNumbers, },
                                    new { Trimmer = justAfterDotTrimmer, BucketPrefixes = suffixes, },
                                };

            Func<ICollection<dynamic>, string[], ICollection<dynamic>> filterToHighestBucket = delegate(ICollection<dynamic> input, string[] buckets)
                {
                    var ret = new List<dynamic>();
                    for (var idx = buckets.Length - 1; idx >= 0; --idx)
                    {
                        var prefixToCheck = buckets[idx];

                        // this is because its a dynamic...
                        Func<dynamic, bool> filterDelegate = (item) => item.SearchString.ToString().StartsWith(prefixToCheck);

                        ret = input.Where(filterDelegate).ToList();

                        if (ret.Any())
                        {
                            return ret;
                        }
                    }

                    throw new NotSupportedException(
                        "The filter list provided did not contain a value that prefix matched any of the seach strings.");
                };

            var theList = wrappedInstanceTypes.Select(_ => (dynamic)_).ToList();
            for (int idx = 0; idx < theMatrix.Length; idx++)
            {
                var item = theMatrix[idx];

                theList = theList.Select(item.Trimmer).ToList();

                var filtered = filterToHighestBucket(theList, item.BucketPrefixes);
                if (filtered.Count == 1)
                {
                    return filtered.Single().InstanceType;
                }
            }

            // since they are all the same final bucket take the random first is fine...
            return theList.First().InstanceType;
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
        public void Terminate(string systemId, string systemLocation)
        {
            var instanceToTerminate = new Instance() { Id = systemId, Region = systemLocation };
            instanceToTerminate.Terminate(this.credentials);
            this.tracker.ProcessInstanceTermination(instanceToTerminate.Id);
        }

        /// <inheritdoc />
        public InstanceDescription Create(string name, DeploymentConfiguration deploymentConfiguration)
        {
            var instanceDetails = this.tracker.CreateInstanceDetails(deploymentConfiguration);

            var imageStrategy = new AmiSearchStrategy()
                                    {
                                        OwnerAlias = instanceDetails.ImageDetails.OwnerAlias,
                                        SearchPattern = instanceDetails.ImageDetails.SearchPattern,
                                        MultipleFoundBehavior =
                                            instanceDetails.ImageDetails.ShouldHaveSingleMatch
                                                ? Enums.MultipleAmiFoundBehavior.Throw
                                                : Enums.MultipleAmiFoundBehavior.FirstSortedDescending,
                                    };

            var namer = new CloudInfrastructureNamer(
                name,
                instanceDetails.ContainerDetails.ContainerLocation);

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

            var instanceToCreate = new Instance()
                                       {
                                           Name = namer.GetInstanceName(),
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
                                           InstanceType = deploymentConfiguration.InstanceType,
                                           DisableApiTermination = false,
                                           MappedVolumes = mappedVolumes,
                                           Region = instanceDetails.Location,
                                           EnableSourceDestinationCheck = true,
                                       };

            var userData = new UserData() { Data = this.GetUserData(name) };

            var createdInstance = instanceToCreate.Create(userData, this.credentials);

            this.tracker.ProcessInstanceCreation(instanceDetails, createdInstance.Id);

            return new InstanceDescription()
                       {
                           Id = createdInstance.Id,
                           Location = createdInstance.Region,
                           PrivateIpAddress = createdInstance.PrivateIpAddress,
                       };
        }

        private string GetUserData(string name)
        {
            return @"
<powershell>
# ADDING RENAME COMMAND BECAUSE 'computerName' WAS PRESENT IN CONFIG
Rename-Computer -NewName '" + name + @"' -Force

# ADDING COMMANDS TO CONFIGURE WINDOWS UPDATE TO RUN EVERYDAY AT 3AM AND INSTALL IMPORTANT UPDATES AUTOMATICALLY
$windowsUpdateSettings = (New-Object -com 'Microsoft.Update.AutoUpdate').Settings
$windowsUpdateSettings.NotificationLevel = 4
$windowsUpdateSettings.Save()

# ADDING COMMANDS TO CONFIGURE TIME TO UPDATE AUTOMATICALLY (on by default but must restart the service)
NET STOP W32Time
NET START W32Time

# ADDING COMMAND TO INSTALL CHOCOLATEY FOR APPLICATION INSTALLS LATER
iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))

# CHOCOLATEY PACKAGES FOR PROFILE: machineProfile-webServer
# PACKAGE - ID: notepadplusplus.install (Notepad++ (Improved Text Editor))
choco install notepadplusplus.install -y
# PACKAGE - ID: GoogleChrome (Chrome Web Browser)
choco install GoogleChrome -7

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

# BLOCK NAME: powershellBlock-enableIIS
# Add IIS and suppporting features
Add-WindowsFeature -IncludeManagementTools -Name Web-Default-Doc, Web-Dir-Browsing, Web-Http-Errors, Web-Static-Content, Web-Http-Redirect, Web-Http-Logging, Web-Custom-Logging, Web-Log-Libraries, Web-Request-Monitor, Web-Http-Tracing, Web-Basic-Auth, Web-Digest-Auth, Web-Windows-Auth, Web-Net-Ext, Web-Net-Ext45, Web-Asp-Net, Web-Asp-Net45, Web-ISAPI-Ext, Web-ISAPI-Filter, Web-Scripting-Tools, NET-Framework-45-ASPNET
# Set IIS Service to restart on failure and reboot on 3rd failure
$services = Get-WMIObject win32_service | Where-Object {$_.name -imatch 'W3SVC' -and $_.startmode -eq 'Auto'}; foreach ($service in $services){sc.exe failure $service.name reset= 86400 actions= restart/5000/restart/5000/reboot/5000}
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
    }
}