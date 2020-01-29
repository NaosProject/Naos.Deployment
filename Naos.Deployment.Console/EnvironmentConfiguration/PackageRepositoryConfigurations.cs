// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRepositoryConfigurations.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System.Collections.Generic;

    using Naos.Packaging.Domain;

    /// <summary>
    /// Consolidated configurations for package repositories.
    /// </summary>
    public class PackageRepositoryConfigurations
    {
        /// <summary>
        /// Gets or sets the configurations to use.
        /// </summary>
        public IReadOnlyCollection<PackageRepositoryConfiguration> Configurations { get; set; }
    }
}