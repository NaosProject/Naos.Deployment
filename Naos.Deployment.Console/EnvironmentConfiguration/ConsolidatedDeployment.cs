// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsolidatedDeployment.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System.Collections.Generic;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Consolidated configurations for package repositories.
    /// </summary>
    public class ConsolidatedDeployment
    {
        /// <summary>
        /// Gets or sets the name to use.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the packages.
        /// </summary>
        public IReadOnlyCollection<PackageDescriptionWithOverrides> Packages { get; set; }

        /// <summary>
        /// Gets or sets the override.
        /// </summary>
        public DeploymentConfiguration DeploymentConfigurationOverride { get; set; }
    }
}
