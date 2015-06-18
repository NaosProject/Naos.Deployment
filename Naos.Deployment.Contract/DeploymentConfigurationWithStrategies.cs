// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationWithStrategies.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    /// <summary>
    /// Deployment configuration with initialization strategies added to specify in config files.
    /// </summary>
    public class DeploymentConfigurationWithStrategies : DeploymentConfiguration, IHaveInitializationStrategies
    {
        /// <inheritdoc />
        public ICollection<InitializationStrategyBase> InitializationStrategies { get; set; }
    }
}
