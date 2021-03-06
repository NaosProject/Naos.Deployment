﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITrackComputingInfrastructure.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Naos.Packaging.Domain;

    /// <summary>
    /// Manages state tracking of various AWS objects for purposes
    ///     of not having to re-discover from AWS API each time and
    ///     also keep additional information not stored there.
    /// </summary>
    public interface ITrackComputingInfrastructure : IDisposable
    {
        /// <summary>
        /// Creates a new arcology.
        /// </summary>
        /// <param name="environment">Environment to create for.</param>
        /// <param name="arcologyInfo"><see cref="ArcologyInfo" /> to use.</param>
        /// <param name="deployedInstances">Optional <see cref="DeployedInstance" />'s that already exist.</param>
        /// <returns>Task for async.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "arcology", Justification = "Spelling/name is correct.")]
        Task Create(string environment, ArcologyInfo arcologyInfo, IReadOnlyCollection<DeployedInstance> deployedInstances = null);

        /// <summary>
        /// Gets instance descriptions of instances that have specified packages deployed to it.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="packages">Packages to look for instances it's deployed to.</param>
        /// <returns>Instance descriptions that have packages deployed to it.</returns>
        Task<IReadOnlyCollection<InstanceDescription>> GetInstancesByDeployedPackagesAsync(string environment, IReadOnlyCollection<PackageDescription> packages);

        /// <summary>
        /// Removes an instance from the tracking system.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="systemId">System ID to remove.</param>
        /// <returns>Task for async execution.</returns>
        Task ProcessInstanceTerminationAsync(string environment, string systemId);

        /// <summary>
        /// Gets instance details necessary to hand off to the computing platform provider.
        /// </summary>
        /// <param name="environment">The environment being deployed to.</param>
        /// <param name="deploymentConfiguration">Deployment requirements.</param>
        /// <param name="intendedPackages">Packages that are planned to be deployed.</param>
        /// <returns>Object holding information necessary to create an instance.</returns>
        Task<InstanceCreationDetails> GetNewInstanceCreationDetailsAsync(
            string environment,
            DeploymentConfiguration deploymentConfiguration,
            IReadOnlyCollection<PackageDescriptionWithOverrides> intendedPackages);

        /// <summary>
        /// Adds the instance to the tracking system.
        /// </summary>
        /// <param name="instanceDescription">Description of the created instance.</param>
        /// <returns>Task for async execution.</returns>
        Task ProcessInstanceCreationAsync(InstanceDescription instanceDescription);

        /// <summary>
        /// Updates the list of the deployed packages.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="systemId">ID from the computing platform provider of the instance.</param>
        /// <param name="package">Package that was successfully deployed.</param>
        /// <returns>Task for async execution.</returns>
        Task ProcessDeployedPackageAsync(string environment, string systemId, PackageDescription package);

        /// <summary>
        /// Gets the instance description by ID.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <returns>InstanceDescription if any by that ID.</returns>
        Task<IReadOnlyCollection<InstanceDescription>> GetAllInstanceDescriptionsAsync(string environment);

        /// <summary>
        /// Gets the instance description by ID.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="systemId">ID from the computing platform provider of the instance.</param>
        /// <returns>InstanceDescription if any by that ID.</returns>
        Task<InstanceDescription> GetInstanceDescriptionByIdAsync(string environment, string systemId);

        /// <summary>
        /// Gets the instance ID by name.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="name">Name of the instance (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <returns>ID of instance by name if found.</returns>
        Task<string> GetInstanceIdByNameAsync(string environment, string name);

        /// <summary>
        /// Looks up the key used for the instance and returns the private key.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="systemId">ID from the computing platform provider of the instance.</param>
        /// <returns>Private key of instance.</returns>
        Task<string> GetPrivateKeyOfInstanceByIdAsync(string environment, string systemId);

        /// <summary>
        /// Get private key for computing container of specific accessibility.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="accessibility">Accessibility to get the private key for.</param>
        /// <returns>Private key of computing container.</returns>
        Task<string> GetPrivateKeyOfComputingContainerAsync(string environment, InstanceAccessibility accessibility);

        /// <summary>
        /// Looks up the hosting ID of the specified ROOT domain (null if not found).
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="domain">Domain to find the hosting ID for (should only be a root domain).</param>
        /// <returns>Hosting ID or null if not found.</returns>
        Task<string> GetDomainZoneIdAsync(string environment, string domain);

        /// <summary>
        /// Removes a previously failed deployment by looking up using the private IP address.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="privateIpAddress">The specified private IP address to use to find the instance.</param>
        /// <returns>Task for async.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        Task ProcessFailedInstanceDeploymentAsync(string environment, string privateIpAddress);

        /// <summary>
        /// Removes an instance from tracking PERMANENTLY by looking up using the private IP address.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="privateIpAddress">The specified private IP address to use to find the instance.</param>
        /// <returns>Task for async.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        Task RemoveInstanceFromTracking(string environment, string privateIpAddress);

        /// <summary>
        /// Looks up the system location for an environment.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <returns>System location.</returns>
        Task<string> GetSystemLocationAsync(string environment);

        /// <summary>
        /// Looks up the IP CIDRs of an environment.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <returns>IP CIDRs.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cidrs", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        Task<IReadOnlyCollection<string>> GetIpAddressCidrsAsync(string environment);
    }

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
        public Task Create(string environment, ArcologyInfo arcologyInfo, IReadOnlyCollection<DeployedInstance> deployedInstances = null)
        {
            return this.emptyTask;
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<InstanceDescription>> GetInstancesByDeployedPackagesAsync(string environment, IReadOnlyCollection<PackageDescription> packages)
        {
            return Task.FromResult<IReadOnlyCollection<InstanceDescription>>(new List<InstanceDescription>());
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
            IReadOnlyCollection<PackageDescriptionWithOverrides> intendedPackages)
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
        public Task<IReadOnlyCollection<InstanceDescription>> GetAllInstanceDescriptionsAsync(string environment)
        {
            return Task.FromResult((IReadOnlyCollection<InstanceDescription>)new List<InstanceDescription>());
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
        public Task<string> GetPrivateKeyOfComputingContainerAsync(string environment, InstanceAccessibility accessibility)
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

        /// <inheritdoc />
        public Task RemoveInstanceFromTracking(string environment, string privateIpAddress)
        {
            return this.emptyTask;
        }

        /// <inheritdoc />
        public Task<string> GetSystemLocationAsync(string environment)
        {
            return Task.FromResult<string>(null);
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> GetIpAddressCidrsAsync(string environment)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(null);
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
