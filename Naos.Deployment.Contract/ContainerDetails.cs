// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContainerDetails.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Object with supporting information about the container an instance should live in.
    /// </summary>
    public class ContainerDetails
    {
        /// <summary>
        /// Gets or sets the environment being serviced by the container.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the configured location.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the ID of the container.
        /// </summary>
        public string ContainerId { get; set; }

        /// <summary>
        /// Gets or sets the location of the container.
        /// </summary>
        public string ContainerLocation { get; set; }

        /// <summary>
        /// Gets or sets the accessibility of the instance.
        /// </summary>
        public InstanceAccessibility InstanceAccessibility { get; set; }

        /// <summary>
        /// Gets or sets the CIDR block of the container.
        /// </summary>
        public string Cidr { get; set; }

        /// <summary>
        /// Gets or sets the where to get the first assignable IP address in the container.
        /// </summary>
        public int StartIpsAfter { get; set; }

        /// <summary>
        /// Gets or sets the configured security group ID.
        /// </summary>
        public string SecurityGroupId { get; set; }

        /// <summary>
        /// Gets or sets the key name of the container.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the private key of the container.
        /// </summary>
        public string PrivateKey { get; set; }
    }
}
