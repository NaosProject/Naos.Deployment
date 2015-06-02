// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagedDeploymentConfiguration.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;

    using Naos.Deployment.Contract;

    /// <summary>
    /// A package with its appropriate deployment configuration.
    /// </summary>
    public class PackagedDeploymentConfiguration
    {
        /// <summary>
        /// Gets or sets the package.
        /// </summary>
        public Package Package { get; set; }

        /// <summary>
        /// Gets or sets the deployment configuration.
        /// </summary>
        public DeploymentConfiguration DeploymentConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the Its.Configuration overrides to apply.
        /// </summary>
        public ICollection<ItsConfigOverride> ItsConfigOverrides { get; set; }
    }
}
