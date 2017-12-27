// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentAdjustmentStrategiesApplicator.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
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
        /// <param name="configFileManager">Configuration file manager.</param>
        /// <param name="packagedDeploymentConfigsWithDefaultsAndOverrides">All package configurations with defaults and overrides applied.</param>
        /// <param name="configToCreateWith">Config to create instance with.</param>
        /// <param name="packageHelper">Package helper.</param>
        /// <param name="itsConfigPrecedenceAfterEnvironment">Its.Configuration precedence chain to apply after the environment.</param>
        /// <param name="setupStepFactorySettings">Setup step factory settings.</param>
        /// <returns>Packages to inject.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        public IReadOnlyCollection<InjectedPackage> IdentifyAdditionalPackages(string environment, string instanceName, int instanceNumber, IManageConfigFiles configFileManager, IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith, PackageHelper packageHelper, string[] itsConfigPrecedenceAfterEnvironment, SetupStepFactorySettings setupStepFactorySettings)
        {
            var packagesToAdd =
                this.DeploymentAdjusters.Where(
                        _ =>
                            _.IsMatch(
                                configFileManager,
                                packagedDeploymentConfigsWithDefaultsAndOverrides,
                                configToCreateWith))
                    .SelectMany(
                        _ =>
                            _.GetAdditionalPackages(
                                environment,
                                instanceName,
                                instanceNumber,
                                configFileManager,
                                packagedDeploymentConfigsWithDefaultsAndOverrides,
                                configToCreateWith,
                                packageHelper,
                                itsConfigPrecedenceAfterEnvironment,
                                setupStepFactorySettings))
                    .ToList();

            return packagesToAdd;
        }
    }
}
