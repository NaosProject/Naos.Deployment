// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceStatus.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;
    using System.Linq;

    using static System.FormattableString;

    /// <summary>
    /// Model object to hold the status of an instance.
    /// </summary>
    public class InstanceStatus
    {
        /// <summary>
        /// Gets or sets the state of the instance.
        /// </summary>
        public InstanceState InstanceState { get; set; }

        /// <summary>
        /// Gets or sets the instance checks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public IDictionary<string, CheckState> InstanceChecks { get; set; }

        /// <summary>
        /// Gets or sets the system checks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public IDictionary<string, CheckState> SystemChecks { get; set; }

        /// <inheritdoc cref="object" />
        public override string ToString()
        {
            var ret = Invariant($"State: {this.InstanceState}; System Checks: {string.Join(",", this.SystemChecks.Select(_ => Invariant($"{_.Key}={_.Value}")))}; Instance Checks: {string.Join(",", this.InstanceChecks.Select(_ => Invariant($"{_.Key}={_.Value}")))}; ");
            return ret;
        }
    }
}