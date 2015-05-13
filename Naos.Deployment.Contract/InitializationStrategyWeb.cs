// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyWeb.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Custom extension of the DeploymentConfiguration to accommodate web service/site deployments.
    /// </summary>
    public class InitializationStrategyWeb : InitializationStrategy
    {
        /// <summary>
        /// Gets or sets the primary DNS access point of the web deployment.
        /// </summary>
        public string PrimaryDns { get; set; }
    }
}
