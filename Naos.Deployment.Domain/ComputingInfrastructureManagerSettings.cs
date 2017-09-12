// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComputingInfrastructureManagerSettings.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Settings to be provided to the ComputingInfrastructureManager (instance type map, etc.).
    /// </summary>
    public class ComputingInfrastructureManagerSettings
    {
        /// <summary>
        /// Gets or sets a map of drive letters to AWS volume descriptors.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public IDictionary<string, string> DriveLetterVolumeDescriptorMap { get; set; }

        /// <summary>
        /// Gets or sets the map of the volume type to the system specific values.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public IDictionary<VolumeType, string> VolumeTypeValueMap { get; set; }

        /// <summary>
        /// Gets or sets a list (in order) of the AWS instance types and their core/RAM details.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Aws", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public ICollection<AwsInstanceType> AwsInstanceTypes { get; set; }

        /// <summary>
        /// Gets or sets a list (in order) of the AWS instance types and their core/RAM details to be used for SQL instances.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Aws", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public ICollection<AwsInstanceType> AwsInstanceTypesForSqlWeb { get; set; }

        /// <summary>
        /// Gets or sets a list (in order) of the AWS instance types and their core/RAM details to be used for SQL instances.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Aws", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public ICollection<AwsInstanceType> AwsInstanceTypesForSqlStandard { get; set; }

        /// <summary>
        /// Gets or sets the user data to use when creating an instance (list allows for keeping multiple lines in JSON format).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public ICollection<string> InstanceCreationUserDataLines { get; set; }

        /// <summary>
        /// Gets or sets a list of package ID's that should be disregarded when looking to replace packages with instance terminations.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public ICollection<string> PackageIdsToIgnoreDuringTerminationSearch { get; set; }

        /// <summary>
        /// Gets or sets the name of the key to use when tagging an instance with its name.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "NameTag", Justification = "Spelling/name is correct.")]
        public string NameTagKey { get; set; }

        /// <summary>
        /// Gets or sets the name of the key to use when tagging an instance with its environment.
        /// </summary>
        public string EnvironmentTagKey { get; set; }

        /// <summary>
        /// Gets or sets the name of the key to use when tagging an instance with the type of Windows OS deployed.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sku", Justification = "Spelling/name is correct.")]
        public string WindowsSkuTagKey { get; set; }

        /// <summary>
        /// Gets or sets the name of the key to use when tagging an instance with whether it's public or private.
        /// </summary>
        public string InstanceAccessibilityTagKey { get; set; }

        /// <summary>
        /// Combines the lines of user data and replaces the token '{ComputerName}' with the name provided.
        /// </summary>
        /// <returns>User data as an un-encoded string to provide to AWS for creating an instance.</returns>
        public string GetInstanceCreationUserData()
        {
            var userData = string.Join(Environment.NewLine, this.InstanceCreationUserDataLines);
            return userData;
        }
    }

    /// <summary>
    /// Settings class with an AWS instance type and its core/RAM details.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Aws", Justification = "Spelling/name is correct.")]
    public class AwsInstanceType
    {
        /// <summary>
        /// Gets or sets the number of cores on the instance type.
        /// </summary>
        public int VirtualCores { get; set; }

        /// <summary>
        /// Gets or sets the amount of RAM on the instance type.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Gb", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Gb", Justification = "Spelling/name is correct.")]
        public double RamInGb { get; set; }

        /// <summary>
        /// Gets or sets the AWS instance type descriptor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Aws", Justification = "Spelling/name is correct.")]
        public string AwsInstanceTypeDescriptor { get; set; }
    }
}
