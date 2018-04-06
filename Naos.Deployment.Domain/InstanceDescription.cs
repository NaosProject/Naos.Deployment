// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceDescription.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Model object of a instance.
    /// </summary>
    public class InstanceDescription
    {
        /// <summary>
        /// Gets or sets the ID (per the computing platform provider) of the instance the task deployed to.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the instance.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the computer name.
        /// </summary>
        public string ComputerName { get; set; }

        /// <summary>
        /// Gets or sets the location (per the computing platform provider) of the instance the task is deployed to.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the environment the instance is deployed to.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the deployed packages on this instance mapped against verification.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Leaving like this for now.")]
        public Dictionary<string, PackageDescriptionWithDeploymentStatus> DeployedPackages { get; set; }

        /// <summary>
        /// Gets or sets the public IP address.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        public string PublicIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the private IP address.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        public string PrivateIpAddress { get; set; }

        /// <summary>
        /// Gets or sets a property bag of system specific details.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public IReadOnlyDictionary<string, string> SystemSpecificDetails { get; set; }

        /// <summary>
        /// Gets or sets the tags on the instance.
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags { get; set; }
    }
}