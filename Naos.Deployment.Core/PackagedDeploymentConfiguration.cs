// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagedDeploymentConfiguration.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    /// <summary>
    /// A package with its appropriate deployment configuration.
    /// </summary>
    public class PackagedDeploymentConfiguration : IHaveInitializationStrategies, IHaveItsConfigOverrides
    {
        /// <summary>
        /// Gets or sets the package.
        /// </summary>
        public Package Package { get; set; }

        /// <summary>
        /// Gets or sets the deployment configuration.
        /// </summary>
        public DeploymentConfiguration DeploymentConfiguration { get; set; }

        /// <inheritdoc />
        public ICollection<InitializationStrategyBase> InitializationStrategies { get; set; }

        /// <inheritdoc />
        public ICollection<ItsConfigOverride> ItsConfigOverrides { get; set; }
    }
}
