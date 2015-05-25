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
        /// <param name="systemId">Proprietary ID of the instance.</param>
        /// <param name="systemLocation">Proprietary location of the instance.</param>
        /// <param name="releasePublicIpIfApplicable">Optionally release the public IP address if the instance has one (DEFAULT is false).</param>
        void Terminate(string systemId, string systemLocation, bool releasePublicIpIfApplicable = false);

        /// <summary>
        /// Creates a new instance per the deployment configuration provided.
        /// </summary>
        /// <param name="name">Name of the instance.</param>
        /// <param name="environment">Environment being deployed to.</param>
        /// <param name="deploymentConfiguration">Deployment configuration to use to build a new instance.</param>
        /// <returns>Description of created instance.</returns>
        InstanceDescription CreateNewInstance(string name, string environment, DeploymentConfiguration deploymentConfiguration);

        /// <summary>
        /// Gets the instance description by the name provided (null if not found).
        /// </summary>
        /// <param name="name">Name of the instance.</param>
        /// <returns>Description of the specified instance or null if not found.</returns>
        InstanceDescription GetInstanceDescription(string name);

        /// <summary>
        /// Gets the administrator password for the specified instance.
        /// </summary>
        /// <param name="instanceDescription">Description of the instance in question.</param>
        /// <param name="privateKey">Decryption key needed for password.</param>
        /// <returns>Password of the instance's administrator account.</returns>
        string GetAdministratorPasswordForInstance(InstanceDescription instanceDescription, string privateKey);

        /// <summary>
        /// Creates or updates the specified DNS entry to the provide IP Addresses.
        /// </summary>
        /// <param name="location">System location to make calls against.</param>
        /// <param name="domain">Domain to operate on.</param>
        /// <param name="ipAddresses">IP Addresses to bind to the DNS entry specified.</param>
        void UpsertDnsEntry(string location, string domain, ICollection<string> ipAddresses);
    }
}
