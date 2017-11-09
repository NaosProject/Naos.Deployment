// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustDeploymentBase.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.ComponentModel;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Interface to allow various types of checks on whether or not to add a package
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public abstract class AdjustDeploymentBase
    {
        /// <summary>
        /// Determines if this adjuster should run on the deployment in question.
        /// </summary>
        /// <param name="configFileManager">Config file manager necessary to be able to provide <see cref="Its.Configuration" /> overrides correctly.</param>
        /// <param name="packagedDeploymentConfigsWithDefaultsAndOverrides">All package configurations with defaults and overrides applied.</param>
        /// <param name="configToCreateWith">Config to create instance with.</param>
        /// <returns>A value indicating whether or not this adjuster is applicable.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        public abstract bool IsMatch(
            IManageConfigFiles configFileManager,
            ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides,
            DeploymentConfiguration configToCreateWith);

        /// <summary>
        /// Builds the additional packages to add to deployment.
        /// </summary>
        /// <param name="environment">Environment being deployed to.</param>
        /// <param name="instanceName">Name of the instance.</param>
        /// <param name="instanceNumber">Instance number (in the multiple instance scenario).</param>
        /// <param name="configFileManager">Config file manager necessary to be able to provide <see cref="Its.Configuration" /> overrides correctly.</param>
        /// <param name="packagedDeploymentConfigsWithDefaultsAndOverrides">All package configurations with defaults and overrides applied.</param>
        /// <param name="configToCreateWith">Config to create instance with.</param>
        /// <param name="packageHelper">Package helper.</param>
        /// <param name="itsConfigPrecedenceAfterEnvironment">Its.Configuration precedence chain to apply after the environment.</param>
        /// <param name="setupStepFactorySettings">Setup step factory settings.</param>
        /// <returns>Set of packages to inject if applicable.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        public abstract IReadOnlyCollection<InjectedPackage> GetAdditionalPackages(
            string environment,
            string instanceName,
            int instanceNumber,
            IManageConfigFiles configFileManager,
            ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides,
            DeploymentConfiguration configToCreateWith,
            PackageHelper packageHelper,
            string[] itsConfigPrecedenceAfterEnvironment,
            SetupStepFactorySettings setupStepFactorySettings);
    }
}