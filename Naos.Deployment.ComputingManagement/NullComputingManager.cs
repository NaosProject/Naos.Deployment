// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullComputingManager.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
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
        public async Task TurnOffInstanceAsync(string systemId, string systemLocation, bool force = false, bool waitUntilOff = true)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }

        /// <inheritdoc />
        public async Task TurnOnInstanceAsync(string systemId, string systemLocation, bool waitUntilOn = true, int maxRebootAttemptsOnFailedStarts = 2)
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
            return new InstanceStatus();
        }

        /// <inheritdoc />
        public async Task<InstanceDescription> CreateNewInstanceAsync(string environment, string name, DeploymentConfiguration deploymentConfiguration, IReadOnlyCollection<PackageDescriptionWithOverrides> intendedPackages, bool includeInstanceInitializationScript)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return new InstanceDescription();
        }

        /// <inheritdoc />
        public Task<InstanceDescription> GetInstanceDescriptionAsync(string environment, string name)
        {
            return null;
        }

        /// <inheritdoc />
        public async Task<IList<InstanceDetailsFromComputingPlatform>> GetActiveInstancesFromProviderAsync(string environment)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return new List<InstanceDetailsFromComputingPlatform>();
        }

        /// <inheritdoc />
        public async Task<string> GetAdministratorPasswordForInstanceAsync(InstanceDescription instanceDescription, string privateKey)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return "NullImplementationPassword";
        }

        /// <inheritdoc />
        public async Task<string> GetConsoleOutputFromInstanceAsync(
            InstanceDescription instanceDescription,
            bool                shouldGetLatest = false)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return "NullImplementationOfConsoleOutput";
        }

        /// <inheritdoc />
        public async Task UpsertDnsEntryAsync(string environment, string location, string domain, IReadOnlyCollection<string> ipAddresses)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
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