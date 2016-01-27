// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullInfrastructureTracker.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.CloudManagement
{
    using System.Collections.Generic;

    using Naos.Deployment.Contract;
    using Naos.Packaging.Domain;

    /// <summary>
    /// Null object implementation for testing.
    /// </summary>
    public class NullInfrastructureTracker : ITrackComputingInfrastructure
    {
        /// <inheritdoc />
        public ICollection<InstanceDescription> GetInstancesByDeployedPackages(string environment, ICollection<PackageDescription> packages)
        {
            return new List<InstanceDescription>();
        }

        /// <inheritdoc />
        public void ProcessInstanceTermination(string environment, string systemId)
        {
        }

        /// <inheritdoc />
        public InstanceCreationDetails GetNewInstanceCreationDetails(
            string environment,
            DeploymentConfiguration deploymentConfiguration,
            ICollection<PackageDescription> intendedPackages)
        {
            return new InstanceCreationDetails();
        }

        /// <inheritdoc />
        public void ProcessInstanceCreation(InstanceDescription instanceDescription)
        {
        }

        /// <inheritdoc />
        public void ProcessDeployedPackage(string environment, string systemId, PackageDescription package)
        {
        }

        /// <inheritdoc />
        public InstanceDescription GetInstanceDescriptionById(string environment, string systemId)
        {
            return null;
        }

        /// <inheritdoc />
        public string GetInstanceIdByName(string environment, string name)
        {
            return null;
        }

        /// <inheritdoc />
        public string GetPrivateKeyOfInstanceById(string environment, string systemId)
        {
            return null;
        }

        /// <inheritdoc />
        public string GetDomainZoneId(string environment, string domain)
        {
            return null;
        }
    }
}
