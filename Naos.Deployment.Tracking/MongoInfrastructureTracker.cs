// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MongoInfrastructureTracker.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
        /// <summary>
        /// This is necessary to make sure we don't do multiple arcology operations at the same time and commit partial results - 1 is the number of allowed threads.
        /// </summary>
        private readonly SemaphoreSlim arcologySemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

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
            // block to make sure only one thread is performing an operation
            await this.arcologySemaphore.WaitAsync();

            try
            {
                var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
                var matchingInstance =
                    arcology.Instances.SingleOrDefault(_ => _.InstanceDescription.Id == systemId);

                // write
                if (matchingInstance != null)
                {
                    arcology.MutateInstancesRemove(matchingInstance);
                    var matchingInstanceContainer = CreateInstanceContainerFromInstance(matchingInstance);
                    await this.instanceCommands.RemoveOneAsync(matchingInstanceContainer);
                }
            }
            finally
            {
                // Release the thread
                this.arcologySemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task<InstanceCreationDetails> GetNewInstanceCreationDetailsAsync(
            string environment,
            DeploymentConfiguration deploymentConfiguration,
            ICollection<PackageDescription> intendedPackages)
        {
            // block to make sure only one thread is performing an operation
            await this.arcologySemaphore.WaitAsync();

            try
            {
                var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
                var newDeployedInstance = arcology.CreateNewDeployedInstance(deploymentConfiguration, intendedPackages);

                // write
                arcology.MutateInstancesAdd(newDeployedInstance);
                var instanceContainer = CreateInstanceContainerFromInstance(newDeployedInstance);
                await this.instanceCommands.AddOrUpdateOneAsync(instanceContainer);

                return newDeployedInstance.InstanceCreationDetails;
            }
            finally
            {
                // Release the thread
                this.arcologySemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task ProcessInstanceCreationAsync(InstanceDescription instanceDescription)
        {
            // block to make sure only one thread is performing an operation
            await this.arcologySemaphore.WaitAsync();

            try
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
                await this.instanceCommands.AddOrUpdateOneAsync(toUpdateContainer);
            }
            finally
            {
                // Release the thread
                this.arcologySemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task ProcessDeployedPackageAsync(string environment, string systemId, PackageDescription package)
        {
            // block to make sure only one thread is performing an operation
            await this.arcologySemaphore.WaitAsync();

            try
            {
                var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
                var instanceToUpdate = arcology.Instances.SingleOrDefault(_ => _.InstanceDescription.Id == systemId);
                if (instanceToUpdate == null)
                {
                    throw new DeploymentException(
                        "Expected to find a tracked instance (post-creation) with system ID: " + systemId);
                }

                // write
                Arcology.UpdatePackageVerificationInInstanceDeploymentList(instanceToUpdate, package);
                var instanceContainer = CreateInstanceContainerFromInstance(instanceToUpdate);
                await this.instanceCommands.AddOrUpdateOneAsync(instanceContainer);
            }
            finally
            {
                // Release the thread
                this.arcologySemaphore.Release();
            }
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

        /// <inheritdoc />
        public async Task ProcessFailedInstanceDeploymentAsync(string environment, string privateIpAddress)
        {
            // block to make sure only one thread is performing an operation
            await this.arcologySemaphore.WaitAsync();

            try
            {
                var arcology = await this.GetArcologyByEnvironmentNameAsync(environment);
                var matchingInstance =
                    arcology.Instances.SingleOrDefault(
                        _ =>
                        _.InstanceDescription.PrivateIpAddress == privateIpAddress
                        && string.IsNullOrEmpty(_.InstanceDescription.Id));

                // write
                if (matchingInstance == null)
                {
                    throw new ArgumentException("Could not find expected instance that failed to deploy (had a null or empty ID); Private IP: " + privateIpAddress);
                }

                arcology.MutateInstancesRemove(matchingInstance);
                var matchingInstanceContainer = CreateInstanceContainerFromInstance(matchingInstance);
                await this.instanceCommands.RemoveOneAsync(matchingInstanceContainer);
            }
            finally
            {
                // Release the thread
                this.arcologySemaphore.Release();
            }
        }

        private async Task<Arcology> GetArcologyByEnvironmentNameAsync(string environment)
        {
            environment = environment ?? "[NULL VALUE PASSED]";
            environment = string.IsNullOrEmpty(environment) ? "[EMPTY STRING PASSED]" : environment;

            var arcologyInfoContainer =
                await
                this.arcologyInfoQueries.GetOneAsync(
                    _ => _.Environment.ToUpperInvariant() == environment.ToUpperInvariant());

            if (arcologyInfoContainer == null)
            {
                throw new ArgumentException("Could not find an arcology definition for environment: " + environment);
            }

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

            var id = $"{environment}--{deployedInstance.InstanceDescription.PrivateIpAddress}";

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
