// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescriptionWithDeploymentStatus.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Package description that can contain a deployment status for use with tracking.
    /// </summary>
    public class PackageDescriptionWithDeploymentStatus : PackageDescription
    {
        /// <summary>
        /// Gets or sets the deployment status of the package.
        /// </summary>
        public PackageDeploymentStatus DeploymentStatus { get; set; }
    }
}