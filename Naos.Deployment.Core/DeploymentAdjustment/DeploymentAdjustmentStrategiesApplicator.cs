// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentAdjustmentStrategiesApplicator.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Model object with necessary details to allow modifications to the deployment if necessary.
    /// </summary>
    public class DeploymentAdjustmentStrategiesApplicator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentAdjustmentStrategiesApplicator"/> class.
        /// </summary>
        /// <param name="deploymentAdjusters">Adjusters to use.</param>
        public DeploymentAdjustmentStrategiesApplicator(IReadOnlyCollection<AdjustDeploymentBase> deploymentAdjusters)
        {
            this.DeploymentAdjusters = deploymentAdjusters;
        }

        /// <summary>
        /// Gets the deployment adjusters.
        /// </summary>
        public IReadOnlyCollection<AdjustDeploymentBase> DeploymentAdjusters { get; private set; }

        /// <summary>
        /// Identify and retrieve the packages to inject into the deployment.
        /// </summary>
        /// <param name="environment">Environment being deployed to.</param>
        /// <param name="instanceName">Name of the instance.</param>
        /// <param name="instanceNumber">Instance number (in the multiple instance scenario).</param>
        /// <param name="packagedDeploymentConfigsWithDefaultsAndOverrides">All package configurations with defaults and overrides applied.</param>
        /// <param name="configToCreateWith">Config to create instance with.</param>
        /// <param name="packageHelper">Package helper.</param>
        /// <param name="itsConfigPrecedenceAfterEnvironment">Its.Configuration precedence chain to apply after the environment.</param>
        /// <param name="rootDeploymentPath">Root deployment path</param>
        /// <returns>Packages to inject.</returns>
        public IReadOnlyCollection<InjectedPackage> IdentifyAdditionalPackages(string environment, string instanceName, int instanceNumber, ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith, PackageHelper packageHelper, string[] itsConfigPrecedenceAfterEnvironment, string rootDeploymentPath)
        {
            var packagesToAdd =
                this.DeploymentAdjusters.Where(
                        _ =>
                            _.IsMatch(
                                packagedDeploymentConfigsWithDefaultsAndOverrides,
                                configToCreateWith))
                    .SelectMany(
                        _ =>
                            _.GetAdditionalPackages(
                                environment,
                                instanceName,
                                instanceNumber,
                                packagedDeploymentConfigsWithDefaultsAndOverrides,
                                configToCreateWith,
                                packageHelper,
                                itsConfigPrecedenceAfterEnvironment,
                                rootDeploymentPath))
                    .ToList();

            return packagesToAdd;
        }
    }
}
