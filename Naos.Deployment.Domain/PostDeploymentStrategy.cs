// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PostDeploymentStrategy.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Information about what to do after the deployment has run.
    /// </summary>
    public class PostDeploymentStrategy
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to shutdown the instance after deployment (otherwise it will be left running).
        /// </summary>
        public bool TurnOffInstance { get; set; }
    }
}