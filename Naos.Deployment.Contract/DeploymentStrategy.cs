// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentStrategy.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Information about how the deployment should run.
    /// </summary>
    public class DeploymentStrategy
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to use the initialization script on launch.
        /// </summary>
        public bool IncludeInstanceInitializationScript { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to run setup steps (instance level OR initialization specific).
        /// </summary>
        public bool RunSetupSteps { get; set; }
    }
}