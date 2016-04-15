// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullInfrastructureTracker.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    /// <summary>
    /// Null object implementation for testing.
    /// </summary>
    public class NullInfrastructureTracker : ITrackComputingInfrastructure
    {
        private readonly Task emptyTask = Task.Run(
            () =>
                {
                });

        /// <inheritdoc />
        public Task<ICollection<InstanceDescription>> GetInstancesByDeployedPackagesAsync(string environment, ICollection<PackageDescription> packages)
        {
            return Task.FromResult<ICollection<InstanceDescription>>(new List<InstanceDescription>());
        }

        /// <inheritdoc />
        public Task ProcessInstanceTerminationAsync(string environment, string systemId)
        {
            return this.emptyTask;
        }

        /// <inheritdoc />
        public Task<InstanceCreationDetails> GetNewInstanceCreationDetailsAsync(
            string environment,
            DeploymentConfiguration deploymentConfiguration,
            ICollection<PackageDescription> intendedPackages)
        {
            return Task.FromResult(new InstanceCreationDetails());
        }

        /// <inheritdoc />
        public Task ProcessInstanceCreationAsync(InstanceDescription instanceDescription)
        {
            return this.emptyTask;
        }

        /// <inheritdoc />
        public Task ProcessDeployedPackageAsync(string environment, string systemId, PackageDescription package)
        {
            return this.emptyTask;
        }

        /// <inheritdoc />
        public Task<InstanceDescription> GetInstanceDescriptionByIdAsync(string environment, string systemId)
        {
            return Task.FromResult<InstanceDescription>(null);
        }

        /// <inheritdoc />
        public Task<string> GetInstanceIdByNameAsync(string environment, string name)
        {
            return Task.FromResult<string>(null);
        }

        /// <inheritdoc />
        public Task<string> GetPrivateKeyOfInstanceByIdAsync(string environment, string systemId)
        {
            return Task.FromResult<string>(null);
        }

        /// <inheritdoc />
        public Task<string> GetDomainZoneIdAsync(string environment, string domain)
        {
            return Task.FromResult<string>(null);
        }

        /// <inheritdoc />
        public Task ProcessFailedInstanceDeploymentAsync(string environment, string privateIpAddress)
        {
            return this.emptyTask;
        }
    }
}
