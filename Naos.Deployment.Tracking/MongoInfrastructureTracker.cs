// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MongoInfrastructureTracker.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

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
        public async Task<ICollection<InstanceDescription>> GetInstancesByDeployedPackagesAsync(string environment, ICollection<PackageDescription> packages)
        {
            var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
            return arcology.GetInstancesByDeployedPackages(packages);
        }

        /// <inheritdoc />
        public async Task ProcessInstanceTerminationAsync(string environment, string systemId)
        {
            var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
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

        /// <inheritdoc />
        public async Task<InstanceCreationDetails> GetNewInstanceCreationDetailsAsync(
            string environment,
            DeploymentConfiguration deploymentConfiguration,
            ICollection<PackageDescription> intendedPackages)
        {
            var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
            var newDeployedInstance = arcology.CreateNewDeployedInstance(deploymentConfiguration, intendedPackages);

            // write
            arcology.MutateInstancesAdd(newDeployedInstance);
            var instanceContainer = CreateInstanceContainerFromInstance(newDeployedInstance);
            await this.instanceCommands.AddOrUpdateOneAsync(instanceContainer);

            return newDeployedInstance.InstanceCreationDetails;
        }

        /// <inheritdoc />
        public async Task ProcessInstanceCreationAsync(InstanceDescription instanceDescription)
        {
            var arcology = await this.GetArcologyByEnvironmentNameAsync(instanceDescription.Environment);

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

        /// <inheritdoc />
        public async Task ProcessDeployedPackageAsync(string environment, string systemId, PackageDescription package)
        {
            var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
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

        /// <inheritdoc />
        public async Task<InstanceDescription> GetInstanceDescriptionByIdAsync(string environment, string systemId)
        {
            var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
            return arcology.GetInstanceDescriptionById(systemId);
        }

        /// <inheritdoc />
        public async Task<string> GetInstanceIdByNameAsync(string environment, string name)
        {
            var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
            return arcology.GetInstanceIdByName(name);
        }

        /// <inheritdoc />
        public async Task<string> GetPrivateKeyOfInstanceByIdAsync(string environment, string systemId)
        {
            var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
            return arcology.GetPrivateKeyOfInstanceById(systemId);
        }

        /// <inheritdoc />
        public async Task<string> GetDomainZoneIdAsync(string environment, string domain)
        {
            var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
            return arcology.GetDomainZoneId(domain);
        }

        private async Task<Arcology> GetArcologyByEnvironmentNameAsync(string environment)
        {
            environment = environment ?? "[NULL VALUE PASSED]";
            environment = string.IsNullOrEmpty(environment) ? "[EMPTY STRING PASSED]" : environment;

            var arcologyInfoContainer =
                await
                this.arcologyInfoQueries.GetOneAsync(
                    _ => _.Environment.ToUpperInvariant() == environment.ToUpperInvariant());

            var instancesContainers =
                await
                this.instanceQueries.GetManyAsync(
                    _ => _.Environment.ToUpperInvariant() == environment.ToUpperInvariant());

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
