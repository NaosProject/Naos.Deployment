// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceType.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    using OBeautifulCode.Math.Recipes;

    /// <summary>
    /// Model object to describe the type/caliber of machine to provision.
    /// </summary>
    public class InstanceType : IEquatable<InstanceType>
    {
        /// <summary>
        /// Gets or sets the minimum number of virtual cores necessary.
        /// </summary>
        public int? VirtualCores { get; set; }

        /// <summary>
        /// Gets or sets the minimum amount of RAM in gigabytes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Gb", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Gb", Justification = "Spelling/name is correct.")]
        public double? RamInGb { get; set; }

        /// <summary>
        /// Gets or sets a description of the intended operating system.
        /// </summary>
        public OperatingSystemDescriptionBase OperatingSystem { get; set; }

        /// <summary>
        /// Gets or sets a specific image to use (must be used in conjunction with WindowsSku.SpecificImageSupplied)
        /// </summary>
        public string SpecificImageSystemId { get; set; }

        /// <summary>
        /// Gets or sets a specific instance type to use (will override VirtualCores and RamInGb settings).
        /// </summary>
        public string SpecificInstanceTypeSystemId { get; set; }

        /// <summary>
        /// Equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if equal.</returns>
        public static bool operator ==(InstanceType first, InstanceType second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return (first.VirtualCores == second.VirtualCores) && (first.RamInGb == second.RamInGb) && (first.SpecificImageSystemId == second.SpecificImageSystemId) && (first.SpecificInstanceTypeSystemId == second.SpecificInstanceTypeSystemId) && (first.OperatingSystem == second.OperatingSystem);
        }

        /// <summary>
        /// Not equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if not equal.</returns>
        public static bool operator !=(InstanceType first, InstanceType second) => !(first == second);

        /// <inheritdoc />
        public bool Equals(InstanceType other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as InstanceType);

        /// <inheritdoc />
        public override int GetHashCode() => HashCodeHelper.Initialize().Hash(this.VirtualCores).Hash(this.RamInGb).Hash(this.SpecificImageSystemId).Hash(this.SpecificInstanceTypeSystemId).Hash(this.OperatingSystem).Value;
    }
}