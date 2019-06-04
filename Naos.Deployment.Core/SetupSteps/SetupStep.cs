// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStep.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using Naos.MachineManagement.Domain;

    /// <summary>
    /// Model object for a setup step when provisioning a box.
    /// </summary>
    internal class SetupStep
    {
        /// <summary>
        /// Gets or sets the description of the step.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the action to run for setup (takes a IManageMachines implementation as a parameter to perform necessary actions remotely).
        /// </summary>
        public Func<IManageMachines, IReadOnlyCollection<dynamic>> SetupFunc { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Description;
        }
    }

    /// <summary>
    /// Model object to store a batch of steps.
    /// </summary>
    internal class SetupStepBatch
    {
        /// <summary>
        /// Gets or sets the order to execute the step.
        /// </summary>
        public ExecutionOrder ExecutionOrder { get; set; }

        /// <summary>
        /// Gets or sets the ordered list of steps to run.
        /// </summary>
        public IReadOnlyCollection<SetupStep> Steps { get; set; }
    }
}
