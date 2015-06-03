// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManageDeployments.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

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
        /// <param name="deploymentConfigOverride">Optional overrides to the deployment configuration.</param>
        void DeployPackages(
            ICollection<PackageDescriptionWithOverrides> packagesToDeploy,
            string environment,
            string instanceName,
            DeploymentConfiguration deploymentConfigOverride = null);
    }
}
