// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MongoInfrastructureTracker.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.Packaging.Domain;

    using Spritely.ReadModel;

    /// <summary>
    /// Tracking system/certificate manager that will use a root folder and will have a folder per environment with a config file and store a file per machine.
    /// </summary>
    public class MongoInfrastructureTracker : ITrackComputingInfrastructure
    {
        private readonly IQueries<ArcologyInfoContainer> arcologyInfoQueries;

        private readonly IQueries<InstanceContainer> instanceQueries;

        private readonly ICommands<string, InstanceContainer> instanceCommands;

        // should maybe break out a lock provider and lock by environment...
        private readonly object sync = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoInfrastructureTracker"/> class.
        /// </summary>
        /// <param name="arcologyInfoQueries">Query interface to get arcology information models.</param>
        /// <param name="instanceQueries">Query interface to get instances.</param>
        /// <param name="instanceCommands">Command interface to add/update/remove instances.</param>
        public MongoInfrastructureTracker(IQueries<ArcologyInfoContainer> arcologyInfoQueries, IQueries<InstanceContainer> instanceQueries, ICommands<string, InstanceContainer> instanceCommands)
        {
            this.arcologyInfoQueries = arcologyInfoQueries;
            this.instanceQueries = instanceQueries;
            this.instanceCommands = instanceCommands;

            BsonClassMapManager.RegisterClassMaps();
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

                // write
                if (matchingInstance != null)
                {
                    arcology.MutateInstancesRemove(matchingInstance);
                    var matchingInstanceContainer = CreateInstanceContainerFromInstance(matchingInstance);
                    this.instanceCommands.RemoveOneAsync(matchingInstanceContainer).Wait();
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
                var newDeployedInstance = arcology.CreateNewDeployedInstance(deploymentConfiguration, intendedPackages);

                // write
                arcology.MutateInstancesAdd(newDeployedInstance);
                var instanceContainer = CreateInstanceContainerFromInstance(newDeployedInstance);
                this.instanceCommands.AddOrUpdateOneAsync(instanceContainer);

                return newDeployedInstance.InstanceCreationDetails;
            }
        }

        /// <inheritdoc />
        public void ProcessInstanceCreation(InstanceDescription instanceDescription)
        {
            lock (this.sync)
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
                var toUpdateContainer = CreateInstanceContainerFromInstance(toUpdate);
                this.instanceCommands.AddOrUpdateOneAsync(toUpdateContainer).Wait();
            }
        }

        /// <inheritdoc />
        public void ProcessDeployedPackage(string environment, string systemId, PackageDescription package)
        {
            lock (this.sync)
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
                var instanceContainer = CreateInstanceContainerFromInstance(instanceToUpdate);
                this.instanceCommands.AddOrUpdateOneAsync(instanceContainer).Wait();
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

            var arcologyInfoTask =
                this.arcologyInfoQueries.GetOneAsync(
                    _ => _.Environment.ToUpperInvariant() == environment.ToUpperInvariant());
            arcologyInfoTask.Wait();
            var arcologyInfoContainer = arcologyInfoTask.Result;

            var instancesTask =
                this.instanceQueries.GetManyAsync(
                    _ => _.Environment.ToUpperInvariant() == environment.ToUpperInvariant());
            instancesTask.Wait();
            var instancesContainers = instancesTask.Result;
            var instances = instancesContainers.Select(_ => _.Instance).ToList();

            var ret = new Arcology(environment, arcologyInfoContainer.ArcologyInfo, instances);

            return ret;
        }

        /// <summary>
        /// Creates a new <see cref="InstanceContainer"/> from a <see cref="DeployedInstance"/>.
        /// </summary>
        /// <param name="deployedInstance">Instance to prepare.</param>
        /// <returns>Prepared instance container.</returns>
        public static InstanceContainer CreateInstanceContainerFromInstance(DeployedInstance deployedInstance)
        {
            var environment = deployedInstance.InstanceDescription.Environment;

            var id = string.Format(
                "{0}--{1}",
                environment,
                deployedInstance.InstanceDescription.PrivateIpAddress);

            var ret = new InstanceContainer
                          {
                              Id = id,
                              Name = deployedInstance.InstanceDescription.Name,
                              Environment = environment,
                              Instance = deployedInstance
                          };

            return ret;
        }
    }
}
