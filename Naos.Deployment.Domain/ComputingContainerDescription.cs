// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComputingContainerDescription.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Object with supporting information about the container an instance should live in.
    /// </summary>
    public class ComputingContainerDescription
    {
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cidr", Justification = "Spelling/name is correct.")]
        public string Cidr { get; set; }

        /// <summary>
        /// Gets or sets the where to get the first assignable IP address in the container.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ips", Justification = "Spelling/name is correct.")]
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
        public string EncryptedPrivateKey { get; set; }

        /// <summary>
        /// Gets or sets the locator for the certificate used to encrypt the private key.
        /// </summary>
        public CertificateLocator EncryptingCertificateLocator { get; set; }
    }
}
