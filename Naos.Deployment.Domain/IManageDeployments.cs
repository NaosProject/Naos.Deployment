// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManageDeployments.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to wrap the full orchestration of a deployment.
    /// </summary>
    public interface IManageDeployments
    {
        /// <summary>
        /// Deploys the specified packages to the specified environment with optional overrides and using the provided friendly name.
        /// </summary>
        /// <param name="packagesToDeploy">Descriptions of packages to deploy.</param>
        /// <param name="environment">Environment from Its.Config to use.</param>
        /// <param name="instanceName">Name of the instance the deployment will reside on.</param>
        /// <param name="existingDeploymentStrategy">Optional strategy for how to handle existing deployments; DEFAULT is Replace.</param>
        /// <param name="deploymentConfigOverride">Optional overrides to the deployment configuration.</param>
        /// <returns>Task for async.</returns>
        Task DeployPackagesAsync(IReadOnlyCollection<PackageDescriptionWithOverrides> packagesToDeploy, string environment, string instanceName, ExistingDeploymentStrategy existingDeploymentStrategy = ExistingDeploymentStrategy.Replace, DeploymentConfiguration deploymentConfigOverride = null);
    }
}
