// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManageCloudInfrastructure.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for performing native cloud operations.
    /// </summary>
    public interface IManageCloudInfrastructure
    {
        /// <summary>
        /// Terminates an instance.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="systemId">Proprietary ID of the instance.</param>
        /// <param name="systemLocation">Proprietary location of the instance.</param>
        /// <param name="releasePublicIpIfApplicable">Optionally release the public IP address if the instance has one (DEFAULT is false).</param>
        void TerminateInstance(string environment, string systemId, string systemLocation, bool releasePublicIpIfApplicable = false);

        /// <summary>
        /// Turns off an instance.
        /// </summary>
        /// <param name="systemId">Proprietary ID of the instance.</param>
        /// <param name="systemLocation">Proprietary location of the instance.</param>
        /// <param name="waitUntilOff">Wait until the machine is off.</param>
        void TurnOffInstance(string systemId, string systemLocation, bool waitUntilOff = true);

        /// <summary>
        /// Turns on an instance.
        /// </summary>
        /// <param name="systemId">Proprietary ID of the instance.</param>
        /// <param name="systemLocation">Proprietary location of the instance.</param>
        /// <param name="waitUntilOn">Wait until the machine is on.</param>
        void TurnOnInstance(string systemId, string systemLocation, bool waitUntilOn = true);

        /// <summary>
        /// Changes the type of an instance.
        /// </summary>
        /// <param name="systemId">Proprietary ID of the instance.</param>
        /// <param name="systemLocation">Proprietary location of the instance.</param>
        /// <param name="newInstanceType">New instance type for the instance.</param>
        void ChangeInstanceType(string systemId, string systemLocation, InstanceType newInstanceType);

        /// <summary>
        /// Creates a new instance per the deployment configuration provided.
        /// </summary>
        /// <param name="environment">Environment being deployed to.</param>
        /// <param name="name">Name of the instance.</param>
        /// <param name="deploymentConfiguration">Deployment configuration to use to build a new instance.</param>
        /// <param name="intendedPackages">Packages that are planned to be deployed.</param>
        /// <param name="includeInstanceInializtionScript">Include the initialization script during creation.</param>
        /// <returns>Description of created instance.</returns>
        InstanceDescription CreateNewInstance(string environment, string name, DeploymentConfiguration deploymentConfiguration, ICollection<PackageDescription> intendedPackages, bool includeInstanceInializtionScript);

        /// <summary>
        /// Gets the instance description by the name provided (null if not found).
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="name">Name of the instance.</param>
        /// <returns>Description of the specified instance or null if not found.</returns>
        InstanceDescription GetInstanceDescription(string environment, string name);

        /// <summary>
        /// Gets the active (not terminated) instances for a given environment and location.
        /// </summary>
        /// <param name="environment">Environment to scope check to.</param>
        /// <param name="systemLocation">System location to make calls against.</param>
        /// <returns>List of instances found in the cloud.</returns>
        IList<InstanceDetailsFromCloud> GetActiveInstancesFromCloud(string environment, string systemLocation);

        /// <summary>
        /// Creates or updates the specified DNS entry to the provide IP Addresses.
        /// </summary>
        /// <param name="environment">Environment to locate correct hosting zone.</param>
        /// <param name="location">System location to make calls against.</param>
        /// <param name="domain">Domain to operate on.</param>
        /// <param name="ipAddresses">IP Addresses to bind to the DNS entry specified.</param>
        void UpsertDnsEntry(string environment, string location, string domain, ICollection<string> ipAddresses);

        /// <summary>
        /// Gets the administrator password for the specified instance.
        /// </summary>
        /// <param name="instanceDescription">Description of the instance in question.</param>
        /// <param name="privateKey">Decryption key needed for password.</param>
        /// <returns>Password of the instance's administrator account.</returns>
        string GetAdministratorPasswordForInstance(InstanceDescription instanceDescription, string privateKey);
    }
}
