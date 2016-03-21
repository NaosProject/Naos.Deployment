// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MongoInstanceFileTracker.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CloudInfrastructureTracking
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;
    using Naos.Deployment.Persistence;
    using Naos.Packaging.Domain;

    /// <summary>
    /// Tracking system/certificate manager that will use a root folder and will have a folder per environment with a config file and store a file per machine.
    /// </summary>
    public class MongoInstanceFileTracker // : ITrackComputingInfrastructure
    {
        /*
        private readonly DeploymentDatabase database;

        private const string InstancePrefix = "Instance--";

        // should maybe break out a lock provider and lock by environment...
        private readonly object sync = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoInstanceFileTracker"/> class.
        /// </summary>
        public MongoInstanceFileTracker(DeploymentDatabase database)
        {
            this.database = database;
        }

        /// <inheritdoc />
        public ICollection<InstanceDescription> GetInstancesByDeployedPackages(string environment, ICollection<PackageDescription> packages)
        {
            lock (this.sync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                return arcology.GetInstancesByDeployedPackages(packages);
            }
        }

        /// <inheritdoc />
        public void ProcessInstanceTermination(string environment, string systemId)
        {
            lock (this.sync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var matchingInstance =
                    arcology.Instances.SingleOrDefault(_ => _.InstanceDescription.Id == systemId);

                // once we have found the file we just need to remove it and commite
                if (matchingInstance != null)
                {
                    var removed = arcology.Instances.Remove(matchingInstance);
                    if (!removed)
                    {
                        throw new ApplicationException("Failed to removed instance that was found for processing termination; ID: " + matchingInstance.InstanceDescription.Id);
                    }

                    this.SaveArcology(arcology);
                }
            }
        }

        /// <inheritdoc />
        public InstanceCreationDetails GetNewInstanceCreationDetails(
            string environment,
            DeploymentConfiguration deploymentConfiguration,
            ICollection<PackageDescription> intendedPackages)
        {
            lock (this.sync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var ret = arcology.MakeNewInstanceCreationDetails(deploymentConfiguration, intendedPackages);
                // write
                this.SaveArcology(arcology);
                return ret;
            }
        }

        /// <inheritdoc />
        public void ProcessInstanceCreation(InstanceDescription instanceDescription)
        {
            lock (this.sync)
            {
                var arcology = this.GetArcologyByEnvironmentName(instanceDescription.Environment);
                arcology.UpdateInstanceDescription(instanceDescription);
                this.SaveArcology(arcology);
            }
        }

        /// <inheritdoc />
        public void ProcessDeployedPackage(string environment, string systemId, PackageDescription package)
        {
            lock (this.sync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                arcology.UpdatePackageVerificationInInstanceDeploymentList(systemId, package);
                this.SaveArcology(arcology);
            }
        }

        /// <inheritdoc />
        public InstanceDescription GetInstanceDescriptionById(string environment, string systemId)
        {
            lock (this.sync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                return arcology.GetInstanceDescriptionById(systemId);
            }
        }

        /// <inheritdoc />
        public string GetInstanceIdByName(string environment, string name)
        {
            lock (this.sync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                return arcology.GetInstanceIdByName(name);
            }
        }

        /// <inheritdoc />
        public string GetPrivateKeyOfInstanceById(string environment, string systemId)
        {
            lock (this.sync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                return arcology.GetPrivateKeyOfInstanceById(systemId);
            }
        }

        /// <inheritdoc />
        public string GetDomainZoneId(string environment, string domain)
        {
            lock (this.sync)
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
            var arcologyFolderPath = this.GetArcologyFolderPath(arcology.Environment);

            foreach (var instanceWrapper in arcology.Instances)
            {
                var instanceFileContents = Serializer.Serialize(instanceWrapper);

                var instanceFilePathIp = GetInstanceFilePathIp(arcologyFolderPath, instanceWrapper);
                if (string.IsNullOrEmpty(instanceWrapper.InstanceDescription.Name))
                {
                    File.WriteAllText(instanceFilePathIp, instanceFileContents);
                }
                else
                {
                    var instanceFilePathNamed = GetInstanceFilePathNamed(arcologyFolderPath, instanceWrapper);

                    File.WriteAllText(instanceFilePathNamed, instanceFileContents);

                    // clean up the file before it had a name (if applicable)
                    if (File.Exists(instanceFilePathIp))
                    {
                        File.Delete(instanceFilePathIp);
                    }
                }
            }
        }
        */
    }
}
