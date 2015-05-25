// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyConsole.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Custom extension of the DeploymentConfiguration to accommodate command line deployments.
    /// </summary>
    public class InitializationStrategyConsole : InitializationStrategy
    {
        /// <summary>
        /// Gets or sets the arguments 
        /// </summary>
        public string Arguments { get; set; }
    }
}
