// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OperatingSystemDescriptionBase.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.ComponentModel;
    using Naos.MachineManagement.Domain;
    using OBeautifulCode.Equality.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Model to represent information about an operating system choice.
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public abstract class OperatingSystemDescriptionBase
    {
        /// <summary>
        /// Gets the protocol to use to communicate with the machine.
        /// </summary>
        public abstract MachineProtocol MachineProtocol { get; }
    }

    /// <summary>
    /// Windows implementation of <see cref="OperatingSystemDescriptionBase" />.
    /// </summary>
    public class OperatingSystemDescriptionWindows : OperatingSystemDescriptionBase, IEquatable<OperatingSystemDescriptionWindows>
    {
        /// <summary>
        /// Gets or sets the Windows SKU to use.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sku", Justification = "Spelling/name is correct.")]
        public WindowsSku Sku { get; set; }

        /// <inheritdoc />
        public override MachineProtocol MachineProtocol => MachineProtocol.WinRm;

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are equal.</returns>
        public static bool operator ==(OperatingSystemDescriptionWindows first, OperatingSystemDescriptionWindows second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return (first.Sku == second.Sku)
                   && (first.MachineProtocol == second.MachineProtocol);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are inequal.</returns>
        public static bool operator !=(OperatingSystemDescriptionWindows first, OperatingSystemDescriptionWindows second) => !(first == second);

        /// <summary>
        /// Checks equality against another object.
        /// </summary>
        /// <param name="other">Other object to check.</param>
        /// <returns>A value indicating whether or not the object is equal.</returns>
        public bool Equals(OperatingSystemDescriptionWindows other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as OperatingSystemDescriptionWindows);

        /// <inheritdoc />
        public override int GetHashCode() => HashCodeHelper.Initialize().Hash(this.Sku).Hash(this.MachineProtocol).Value;

        /// <inheritdoc />
        public override string ToString()
        {
            return Invariant($"{nameof(WindowsSku)}-{this.Sku}");
        }
    }

    /// <summary>
    /// Linux implementation of <see cref="OperatingSystemDescriptionBase" />.
    /// </summary>
    public class OperatingSystemDescriptionLinux : OperatingSystemDescriptionBase
    {
        /// <summary>
        /// Gets or sets the distribution to use.
        /// </summary>
        public LinuxDistribution Distribution { get; set; }

        /// <inheritdoc />
        public override MachineProtocol MachineProtocol => MachineProtocol.Ssh;

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are equal.</returns>
        public static bool operator ==(OperatingSystemDescriptionLinux first, OperatingSystemDescriptionLinux second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return (first.Distribution == second.Distribution)
                   && (first.MachineProtocol == second.MachineProtocol);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="first">First parameter.</param>
        /// <param name="second">Second parameter.</param>
        /// <returns>A value indicating whether or not the two items are inequal.</returns>
        public static bool operator !=(OperatingSystemDescriptionLinux first, OperatingSystemDescriptionLinux second) => !(first == second);

        /// <summary>
        /// Checks equality against another object.
        /// </summary>
        /// <param name="other">Other object to check.</param>
        /// <returns>A value indicating whether or not the object is equal.</returns>
        public bool Equals(OperatingSystemDescriptionLinux other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as OperatingSystemDescriptionLinux);

        /// <inheritdoc />
        public override int GetHashCode() => HashCodeHelper.Initialize().Hash(this.Distribution).Hash(this.MachineProtocol).Value;

        /// <inheritdoc />
        public override string ToString()
        {
            return Invariant($"{nameof(LinuxDistribution)}-{this.Distribution}");
        }
    }
}
