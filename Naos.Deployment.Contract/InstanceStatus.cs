// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceStatus.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
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
        public IDictionary<string, CheckState> InstanceChecks { get; set; }

        /// <summary>
        /// Gets or sets the system checks.
        /// </summary>
        public IDictionary<string, CheckState> SystemChecks { get; set; }
    }
}