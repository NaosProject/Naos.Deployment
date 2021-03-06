﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Volume.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    using OBeautifulCode.Equality.Recipes;

    /// <summary>
    /// Object to describe a volume to attach to the instance.
    /// </summary>
    public class Volume : IEquatable<Volume>
    {
        /// <summary>
        /// Gets or sets the drive letter of the volume.
        /// </summary>
        public string DriveLetter { get; set; }

        /// <summary>
        /// Gets or sets the size of the volume in gigabytes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Gb", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Gb", Justification = "Spelling/name is correct.")]
        public int SizeInGb { get; set; }

        /// <summary>
        /// Gets or sets the type of volume.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Spelling/name is correct.")]
        public VolumeType Type { get; set; }

        /// <summary>
        /// Equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if equal.</returns>
        public static bool operator ==(Volume first, Volume second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return (first.DriveLetter == second.DriveLetter) && (first.SizeInGb == second.SizeInGb) && (first.Type == second.Type);
        }

        /// <summary>
        /// Not equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if not equal.</returns>
        public static bool operator !=(Volume first, Volume second) => !(first == second);

        /// <inheritdoc />
        public bool Equals(Volume other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as Volume);

        /// <inheritdoc />
        public override int GetHashCode() => HashCodeHelper.Initialize().Hash(this.DriveLetter).Hash(this.SizeInGb).Hash(this.Type).Value;
    }
}
