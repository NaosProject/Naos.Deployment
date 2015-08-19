// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RootFolderEnvironmentFolderInstanceFileTracker.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CloudInfrastructureTracking
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Tracking system/certificate manager that will use a root folder and will have a folder per environment with a config file and store a file per machine.
    /// </summary>
    public class RootFolderEnvironmentFolderInstanceFileTracker : ITrackComputingInfrastructure
    {
        private const string InstancePrefix = "Instance--";

        // should maybe break out a lock provider and lock by environment...
        private readonly object fileSync = new object();

        private readonly string rootFolderPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="RootFolderEnvironmentFolderInstanceFileTracker"/> class.
        /// </summary>
        /// <param name="rootFolderPath">Path to save all state to.</param>
        public RootFolderEnvironmentFolderInstanceFileTracker(string rootFolderPath)
        {
            this.rootFolderPath = rootFolderPath;
        }

        /// <inheritdoc />
        public ICollection<InstanceDescription> GetInstancesByDeployedPackages(string environment, ICollection<PackageDescription> packages)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                return arcology.GetInstancesByDeployedPackages(packages);
            }
        }

        /// <inheritdoc />
        public void ProcessInstanceTermination(string environment, string systemId)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var matchingInstance =
                    arcology.Instances.SingleOrDefault(_ => _.InstanceDescription.Id == systemId);

                // once we have found the file we just need to delete the file
                if (matchingInstance != null)
                {
                    var instanceFile = Path.Combine(
                        this.rootFolderPath,
                        arcology.Environment,
                        InstancePrefix + matchingInstance.InstanceDescription.Name + ".json");
                    if (File.Exists(instanceFile))
                    {
                        File.Delete(instanceFile);
                    }
                }
            }
        }

        /// <inheritdoc />
        public InstanceCreationDetails GetNewInstanceCreationDetails(
            string environment,
            DeploymentConfiguration deploymentConfiguration,
            ICollection<PackageDescription> intendedPackages)
        {
            var arcology = this.GetArcologyByEnvironmentName(environment);
            var ret = arcology.MakeNewInstanceCreationDetails(deploymentConfiguration, intendedPackages);
            this.SaveArcology(arcology);
            return ret;
        }

        /// <inheritdoc />
        public void ProcessInstanceCreation(InstanceDescription instanceDescription)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(instanceDescription.Environment);
                arcology.UpdateInstanceDescription(instanceDescription);
                this.SaveArcology(arcology);
            }
        }

        /// <inheritdoc />
        public void ProcessDeployedPackage(string environment, string systemId, PackageDescription package)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                arcology.UpdatePackageVerificationInInstanceDeploymentList(systemId, package);
                this.SaveArcology(arcology);
            }
        }

        /// <inheritdoc />
        public InstanceDescription GetInstanceDescriptionById(string environment, string systemId)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                return arcology.GetInstanceDescriptionById(systemId);
            }
        }

        /// <inheritdoc />
        public string GetInstanceIdByName(string environment, string name)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                return arcology.GetInstanceIdByName(name);
            }
        }

        /// <inheritdoc />
        public string GetPrivateKeyOfInstanceById(string environment, string systemId)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                return arcology.GetPrivateKeyOfInstanceById(systemId);
            }
        }

        /// <inheritdoc />
        public string GetDomainZoneId(string environment, string domain)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                return arcology.GetDomainZoneId(domain);
            }
        }

        private Arcology GetArcologyByEnvironmentName(string environment)
        {
            environment = environment ?? "[NULL VALUE PASSED]";
            environment = string.IsNullOrEmpty(environment) ? "[EMPTY STRING PASSED]" : environment;

            var arcologyFolderPath = this.GetArcologyFolderPath(environment);
            if (!Directory.Exists(arcologyFolderPath))
            {
                throw new ArgumentException(
                    "Failed to find tracking information for environment: " + " expected information at: " + arcologyFolderPath);
            }

            var instanceFiles = Directory.GetFiles(arcologyFolderPath, InstancePrefix + "*", SearchOption.TopDirectoryOnly);
            var instances = instanceFiles.Select(_ => Serializer.Deserialize<InstanceWrapper>(File.ReadAllText(_))).ToList();
            var arcologyInfoFilePath = Path.Combine(arcologyFolderPath, "ArcologyInfo.json");
            var arcologyInfoText = File.ReadAllText(arcologyInfoFilePath);
            var arcologyInfo = Serializer.Deserialize<ArcologyInfo>(arcologyInfoText);

            var ret = new Arcology
                          {
                              Environment = environment,
                              CloudContainers = arcologyInfo.CloudContainers,
                              RootDomainHostingIdMap = arcologyInfo.RootDomainHostingIdMap,
                              Location = arcologyInfo.Location,
                              WindowsSkuSearchPatternMap = arcologyInfo.WindowsSkuSearchPatternMap,
                              Instances = instances
                          };

            return ret;
        }

        private void SaveArcology(Arcology arcology)
        {
            var namedIpAddresses = new List<string>();
            var arcologyFolderPath = this.GetArcologyFolderPath(arcology.Environment);

            foreach (var instanceWrapper in arcology.Instances)
            {
                var instanceFilePath = Path.Combine(
                    arcologyFolderPath,
                    InstancePrefix + instanceWrapper.InstanceDescription.Name + ".json");

                if (!string.IsNullOrEmpty(instanceWrapper.InstanceDescription.Name))
                {
                    namedIpAddresses.Add(instanceWrapper.InstanceDescription.PrivateIpAddress);
                }

                var instanceFileContents = Serializer.Serialize(instanceWrapper);
                File.WriteAllText(instanceFilePath, instanceFileContents);
            }

            // files for new instances will be created nameless and should get recreated with name and thus need the remnants cleaned up...
            var namelessFiles = Directory.GetFiles(
                arcologyFolderPath,
                InstancePrefix + ".json",
                SearchOption.TopDirectoryOnly);
            var namelessFilesContent = namelessFiles.Select(_ => new { FilePath = _, FileText = File.ReadAllText(_) });

            var namelessItemsToProcess =
                namelessFilesContent.Select(
                    _ =>
                    new
                        {
                            IpAddress = Serializer.Deserialize<InstanceWrapper>(_.FileText).InstanceDescription.PrivateIpAddress,
                            FilePath = _.FilePath,
                        });

            foreach (var namelessItem in namelessItemsToProcess)
            {
                if (namedIpAddresses.Contains(namelessItem.IpAddress))
                {
                    // this has been updated and written in a named file, delete this file...
                    File.Delete(namelessItem.FilePath);
                }
            }
        }

        private string GetArcologyFolderPath(string environment)
        {
            var arcologyFolderPath = Path.Combine(this.rootFolderPath, environment);
            return arcologyFolderPath;
        }
    }
}
