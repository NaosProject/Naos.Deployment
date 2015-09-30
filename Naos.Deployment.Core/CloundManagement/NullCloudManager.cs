// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullCloudManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;

    using Naos.Deployment.Contract;

    /// <inheritdoc />
    public class NullCloudManager : IManageCloudInfrastructure
    {
        /// <inheritdoc />
        public void Terminate(string environment, string systemId, string systemLocation, bool releasePublicIpIfApplicable = false)
        {
        }

        /// <inheritdoc />
        public InstanceDescription CreateNewInstance(string environment, string name, DeploymentConfiguration deploymentConfiguration, ICollection<PackageDescription> intendedPackages)
        {
            return new InstanceDescription();
        }

        /// <inheritdoc />
        public InstanceDescription GetInstanceDescription(string environment, string name)
        {
            return null;
        }

        /// <inheritdoc />
        public string GetAdministratorPasswordForInstance(InstanceDescription instanceDescription, string privateKey)
        {
            return null;
        }

        /// <inheritdoc />
        public void UpsertDnsEntry(string environment, string location, string domain, ICollection<string> ipAddresses)
        {
        }
    }
}