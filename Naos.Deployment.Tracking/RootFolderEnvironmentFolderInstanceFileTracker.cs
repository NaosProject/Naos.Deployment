// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RootFolderEnvironmentFolderInstanceFileTracker.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;
    using Naos.Serialization.Domain;
    using Naos.Serialization.Json;

    using static System.FormattableString;

    /// <summary>
    /// Tracking system/certificate manager that will use a root folder and will have a folder per environment with a config file and store a file per machine.
    /// </summary>
    public class RootFolderEnvironmentFolderInstanceFileTracker : ITrackComputingInfrastructure
    {
        private const string InstancePrefix = "Instance--";
        private const string IpInfix = "ip--";

        private static readonly ISerializeAndDeserialize Serializer = new NaosJsonSerializer();

        private readonly Task emptyTask = Task.Run(
            () =>
                {
                });

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
        public async Task Create(string environment, ArcologyInfo arcologyInfo, IReadOnlyCollection<DeployedInstance> deployedInstances = null)
        {
            await Task.Run(
                () =>
                    {
                        /* no-op */
                    });

            lock (this.fileSync)
            {
                var arcologyFolderPath = this.GetArcologyFolderPath(environment);
                if (!Directory.Exists(arcologyFolderPath))
                {
                    Directory.CreateDirectory(arcologyFolderPath);
                }

                var arcologyInfoJson = Serializer.SerializeToString(arcologyInfo);

                var arcologyInfoFilePath = Path.Combine(arcologyFolderPath, Invariant($"{nameof(ArcologyInfo)}.json"));
                File.WriteAllText(arcologyInfoFilePath, arcologyInfoJson);

                var arcology = new Arcology(environment, arcologyInfo, deployedInstances);
                this.SaveArcology(arcology);
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<InstanceDescription>> GetInstancesByDeployedPackagesAsync(string environment, IReadOnlyCollection<PackageDescription> packages)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var ret = arcology.GetInstancesByDeployedPackages(packages);
                return Task.FromResult(ret);
            }
        }

        /// <inheritdoc />
        public Task ProcessInstanceTerminationAsync(string environment, string systemId)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var matchingInstance =
                    arcology.Instances.SingleOrDefault(_ => _.InstanceDescription.Id == systemId);

                // once we have found the file we just need to delete the file
                if (matchingInstance != null)
                {
                    var arcologyFolderPath = this.GetArcologyFolderPath(arcology.Environment);
                    var instanceFilePathNamed = GetInstanceFilePathNamed(arcologyFolderPath, matchingInstance);
                    var instanceFilePathIp = GetInstanceFilePathIp(arcologyFolderPath, matchingInstance);

                    if (File.Exists(instanceFilePathNamed))
                    {
                        File.Delete(instanceFilePathNamed);
                    }

                    // clean up the file before it had a name (if applicable)
                    if (File.Exists(instanceFilePathIp))
                    {
                        File.Delete(instanceFilePathIp);
                    }
                }

                return this.emptyTask;
            }
        }

        /// <inheritdoc />
        public Task<InstanceCreationDetails> GetNewInstanceCreationDetailsAsync(
            string environment,
            DeploymentConfiguration deploymentConfiguration,
            IReadOnlyCollection<PackageDescriptionWithOverrides> intendedPackages)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var newDeployedInstance = arcology.CreateNewDeployedInstance(deploymentConfiguration, intendedPackages);

                // write
                arcology.MutateInstancesAdd(newDeployedInstance);
                this.SaveArcology(arcology);

                return Task.FromResult(newDeployedInstance.InstanceCreationDetails);
            }
        }

        /// <inheritdoc />
        public Task ProcessInstanceCreationAsync(InstanceDescription instanceDescription)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(instanceDescription.Environment);

                var toUpdate =
                    arcology.Instances.SingleOrDefault(
                        _ => _.InstanceCreationDetails.PrivateIpAddress == instanceDescription.PrivateIpAddress);

                if (toUpdate == null)
                {
                    throw new DeploymentException(
                        "Expected to find a tracked instance (pre-creation) with private IP: "
                        + instanceDescription.PrivateIpAddress);
                }

                // write
                toUpdate.InstanceDescription = instanceDescription;
                this.SaveArcology(arcology);

                return this.emptyTask;
            }
        }

        /// <inheritdoc />
        public Task ProcessDeployedPackageAsync(string environment, string systemId, PackageDescription package)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var instanceToUpdate = arcology.Instances.SingleOrDefault(_ => _.InstanceDescription.Id == systemId);
                if (instanceToUpdate == null)
                {
                    throw new DeploymentException(
                        "Expected to find a tracked instance (post-creation) with system ID: "
                        + systemId);
                }

                // write
                Arcology.UpdatePackageVerificationInInstanceDeploymentList(instanceToUpdate, package);
                this.SaveArcology(arcology);

                return this.emptyTask;
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<InstanceDescription>> GetAllInstanceDescriptionsAsync(string environment)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                IReadOnlyCollection<InstanceDescription> ret = arcology.Instances.Select(_ => _.InstanceDescription).ToList();

                return Task.FromResult(ret);
            }
        }

        /// <inheritdoc />
        public Task<InstanceDescription> GetInstanceDescriptionByIdAsync(string environment, string systemId)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var ret = arcology.GetInstanceDescriptionById(systemId);
                return Task.FromResult(ret);
            }
        }

        /// <inheritdoc />
        public Task<string> GetInstanceIdByNameAsync(string environment, string name)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var ret = arcology.GetInstanceIdByName(name);
                return Task.FromResult(ret);
            }
        }

        /// <inheritdoc />
        public Task<string> GetPrivateKeyOfInstanceByIdAsync(string environment, string systemId)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var ret = arcology.GetPrivateKeyOfInstanceById(systemId);
                return Task.FromResult(ret);
            }
        }

        /// <inheritdoc />
        public Task<string> GetPrivateKeyOfComputingContainerAsync(string environment, InstanceAccessibility accessibility)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var ret = arcology.GetPrivateKeyOfComputingContainer(accessibility);
                return Task.FromResult(ret);
            }
        }

        /// <inheritdoc />
        public Task<string> GetDomainZoneIdAsync(string environment, string domain)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var ret = arcology.GetDomainZoneId(domain);
                return Task.FromResult(ret);
            }
        }

        /// <inheritdoc />
        public Task ProcessFailedInstanceDeploymentAsync(string environment, string privateIpAddress)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var matchingInstance = arcology.Instances.SingleOrDefault(
                    _ => _.InstanceDescription.PrivateIpAddress == privateIpAddress && string.IsNullOrEmpty(_.InstanceDescription.Id));

                // once we have found the file we just need to delete the file
                if (matchingInstance == null)
                {
                    throw new ArgumentException(
                        "Could not find expected instance that failed to deploy (had a null or empty ID); Private IP: " + privateIpAddress);
                }

                var arcologyFolderPath = this.GetArcologyFolderPath(arcology.Environment);
                var instanceFilePathNamed = GetInstanceFilePathNamed(arcologyFolderPath, matchingInstance);
                var instanceFilePathIp = GetInstanceFilePathIp(arcologyFolderPath, matchingInstance);

                if (File.Exists(instanceFilePathNamed))
                {
                    File.Delete(instanceFilePathNamed);
                }

                // clean up the file before it had a name (if applicable)
                if (File.Exists(instanceFilePathIp))
                {
                    File.Delete(instanceFilePathIp);
                }

                return this.emptyTask;
            }
        }

        /// <inheritdoc />
        public Task RemoveInstanceFromTracking(string environment, string privateIpAddress)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var matchingInstance =
                    arcology.Instances.SingleOrDefault(_ => _.InstanceDescription.PrivateIpAddress == privateIpAddress);

                // once we have found the file we just need to delete the file
                if (matchingInstance == null)
                {
                    throw new ArgumentException(Invariant($"Could not find instance by IP address: {privateIpAddress}."));
                }

                var arcologyFolderPath = this.GetArcologyFolderPath(arcology.Environment);
                var instanceFilePathNamed = GetInstanceFilePathNamed(arcologyFolderPath, matchingInstance);
                var instanceFilePathIp = GetInstanceFilePathIp(arcologyFolderPath, matchingInstance);

                if (File.Exists(instanceFilePathNamed))
                {
                    File.Delete(instanceFilePathNamed);
                }

                // clean up the file before it had a name (if applicable)
                if (File.Exists(instanceFilePathIp))
                {
                    File.Delete(instanceFilePathIp);
                }

                return this.emptyTask;
            }
        }

        /// <inheritdoc />
        public Task<string> GetSystemLocationAsync(string environment)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var ret = arcology.Location;
                return Task.FromResult(ret);
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> GetIpAddressCidrsAsync(string environment)
        {
            lock (this.fileSync)
            {
                var arcology = this.GetArcologyByEnvironmentName(environment);
                var ret = arcology.ComputingContainers.Select(_ => _.Cidr).ToList();
                return Task.FromResult<IReadOnlyCollection<string>>(ret);
            }
        }

        /// <summary>
        /// Gets a copy of the arcology by environment.
        /// </summary>
        /// <param name="environment">The environment to get an arcology for.</param>
        /// <returns>The arcology for the provided environment.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arcology", Justification = "Spelling/name is correct.")]
        public Arcology GetArcologyByEnvironmentName(string environment)
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
            var instances = instanceFiles.Select(_ => Serializer.Deserialize<DeployedInstance>(File.ReadAllText(_))).ToList();
            var arcologyInfoFilePath = Path.Combine(arcologyFolderPath, Invariant($"{nameof(ArcologyInfo)}.json"));
            var arcologyInfoText = File.ReadAllText(arcologyInfoFilePath);
            var arcologyInfo = Serializer.Deserialize<ArcologyInfo>(arcologyInfoText);

            var ret = new Arcology(environment, arcologyInfo, instances);
            return ret;
        }

        private void SaveArcology(Arcology arcology)
        {
            var arcologyFolderPath = this.GetArcologyFolderPath(arcology.Environment);
            if (!Directory.Exists(arcologyFolderPath))
            {
                Directory.CreateDirectory(arcologyFolderPath);
            }

            foreach (var instanceWrapper in arcology.Instances)
            {
                var instanceFileContents = Serializer.SerializeToString(instanceWrapper);

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

        private static string GetInstanceFilePathIp(string arcologyFolderPath, DeployedInstance deployedInstance)
        {
            var instanceFilePathIp = Path.Combine(
                arcologyFolderPath,
                InstancePrefix + IpInfix + deployedInstance.InstanceDescription.PrivateIpAddress + ".json");
            return instanceFilePathIp;
        }

        private static string GetInstanceFilePathNamed(string arcologyFolderPath, DeployedInstance deployedInstance)
        {
            var instanceFilePathNamed = Path.Combine(
                arcologyFolderPath,
                InstancePrefix + deployedInstance.InstanceDescription.Name + ".json");
            return instanceFilePathNamed;
        }

        private string GetArcologyFolderPath(string environment)
        {
            var arcologyFolderPath = Path.Combine(this.rootFolderPath, environment);
            return arcologyFolderPath;
        }

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Dispose method.
        /// </summary>
        /// <param name="disposing">Value indicating whether or not it is disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // Dispose code goes here
                    /* no-op */
                }

                this.disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            // Don't change this code
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
