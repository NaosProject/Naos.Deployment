// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStep.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;

    using Naos.WinRM;

    /// <summary>
    /// Model object for a setup step when provisioning a box.
    /// </summary>
    public class SetupStep
    {
        /// <summary>
        /// Gets or sets the description of the step.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the action to run for setup (takes a IManageMachines implementation as a parameter to perform necessary actions remotely).
        /// </summary>
        public Func<IManageMachines, ICollection<dynamic>> SetupFunc { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Description;
        }
    }
}
