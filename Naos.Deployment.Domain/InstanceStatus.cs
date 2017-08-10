// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceStatus.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;

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
    }
}