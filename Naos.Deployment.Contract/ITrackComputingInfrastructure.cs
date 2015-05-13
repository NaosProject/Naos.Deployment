// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITrackComputingInfrastructure.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    /// <summary>
    /// Manages state tracking of various AWS objects for purposes
    ///     of not having to re-discover from AWS API each time and
    ///     also keep additional information not stored there.
    /// </summary>
    public interface ITrackComputingInfrastructure
    {
        /// <summary>
        /// Gets instance descriptions of instances that have specified packages deployed to it.
        /// </summary>
        /// <param name="packages">Packages to look for instances it's deployed to.</param>
        /// <returns>Instance descriptions that have packages deployed to it.</returns>
        ICollection<InstanceDescription> GetInstancesByDeployedPackages(ICollection<PackageDescription> packages);

        /// <summary>
        /// Removes an instance from the tracking system.
        /// </summary>
        /// <param name="systemId">System ID to remove.</param>
        void ProcessInstanceTermination(string systemId);

        /// <summary>
        /// Gets instance details necessary to hand off to the cloud provider.
        /// </summary>
        /// <param name="deploymentConfiguration">Deployment requirements.</param>
        /// <returns>Object holding information necessary to create an instance.</returns>
        InstanceDetails CreateInstanceDetails(DeploymentConfiguration deploymentConfiguration);

        /// <summary>
        /// Adds the instance to the tracking system.
        /// </summary>
        /// <param name="instanceDetails">Details the tracking system provided around provisioning an instance.</param>
        /// <param name="systemId">ID from the cloud provider of the instance.</param>
        void ProcessInstanceCreation(InstanceDetails instanceDetails, string systemId);

        /// <summary>
        /// Updates the list of the deployed packages.
        /// </summary>
        /// <param name="systemId">ID from the cloud provider of the instance.</param>
        /// <param name="package">Package that was successfully deployed.</param>
        void ProcessDeployedPackage(string systemId, PackageDescription package);

        /// <summary>
        /// Gets the instance description by ID.
        /// </summary>
        /// <param name="systemId">ID from the cloud provider of the instance.</param>
        /// <returns>InstanceDescription if any by that ID.</returns>
        InstanceDescription GetInstanceDescriptionById(string systemId);

        /// <summary>
        /// Looks up the key used for the instance and returns the private key.
        /// </summary>
        /// <param name="systemId">ID from the cloud provider of the instance.</param>
        /// <returns>Private key of instance.</returns>
        string GetPrivateKeyOfInstanceById(string systemId);
    }
}
