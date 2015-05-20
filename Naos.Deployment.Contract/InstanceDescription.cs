// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceDescription.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    /// <summary>
    /// Model object of a instance.
    /// </summary>
    public class InstanceDescription
    {
        /// <summary>
        /// Gets or sets the ID (per the cloud provider) of the instance the task deployed to.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the instance.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the location (per the cloud provider) of the instance the task is deployed to.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the environment the instance is deployed to.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the deployed packages on this instance.
        /// </summary>
        public ICollection<PackageDescription> DeployedPackages { get; set; }

        /// <summary>
        /// Gets or sets the public IP address.
        /// </summary>
        public string PublicIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the private IP address.
        /// </summary>
        public string PrivateIpAddress { get; set; }

        /// <summary>
        /// Gets or sets a property bag of system specific details.
        /// </summary>
        public Dictionary<string, string> SystemSpecificDetails { get; set; }
    }
}