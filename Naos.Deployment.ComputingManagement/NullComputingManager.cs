// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullComputingManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.ComputingManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    /// <inheritdoc />
    public class NullComputingManager : IManageComputingInfrastructure
    {
        /// <inheritdoc />
        public async Task TerminateInstanceAsync(string environment, string systemId, string systemLocation, bool releasePublicIpIfApplicable = false)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }

        /// <inheritdoc />
        public async Task TurnOffInstanceAsync(string systemId, string systemLocation, bool waitUntilOff = true)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }

        /// <inheritdoc />
        public async Task TurnOnInstanceAsync(string systemId, string systemLocation, bool waitUntilOn = true)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }

        /// <inheritdoc />
        public async Task ChangeInstanceTypeAsync(string systemId, string systemLocation, InstanceType newInstanceType)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }

        /// <inheritdoc />
        public async Task<InstanceStatus> GetInstanceStatusAsync(string systemId, string systemLocation)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return null;
        }

        /// <inheritdoc />
        public async Task<InstanceDescription> CreateNewInstanceAsync(string environment, string name, DeploymentConfiguration deploymentConfiguration, ICollection<PackageDescription> intendedPackages, bool includeInstanceInializtionScript)
        {
            return await Task.FromResult(new InstanceDescription());
        }

        /// <inheritdoc />
        public InstanceDescription GetInstanceDescription(string environment, string name)
        {
            return null;
        }

        /// <inheritdoc />
        public async Task<IList<InstanceDetailsFromComputingPlatform>> GetActiveInstancesFromProviderAsync(string environment, string systemLocation)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return new List<InstanceDetailsFromComputingPlatform>();
        }

        /// <inheritdoc />
        public async Task<string> GetAdministratorPasswordForInstanceAsync(InstanceDescription instanceDescription, string privateKey)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return null;
        }

        /// <inheritdoc />
        public async Task UpsertDnsEntryAsync(string environment, string location, string domain, ICollection<string> ipAddresses)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }
    }
}