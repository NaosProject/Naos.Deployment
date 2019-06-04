// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagedDeploymentConfigurationExtensionMethods.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;

    using OBeautifulCode.Validation.Recipes;

    /// <summary>
    /// Additional behavior to add the initialization strategies.
    /// </summary>
    public static class PackagedDeploymentConfigurationExtensionMethods
    {
        /// <summary>
        /// Apply default deployment configuration to list of packaged configurations (overwrite missing properties).
        /// </summary>
        /// <param name="packagedConfigs">List of configurations to check.</param>
        /// <param name="defaultDeploymentConfig">Default configuration to use for missing properties.</param>
        /// <returns>List of configurations with defaults applied.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        public static IReadOnlyCollection<PackagedDeploymentConfiguration> ApplyDefaults(
            this IReadOnlyCollection<PackagedDeploymentConfiguration> packagedConfigs,
            DeploymentConfiguration defaultDeploymentConfig)
        {
            new { packagedConfigs }.Must().NotBeNull();

            if (packagedConfigs.Count == 0)
            {
                return
                    new[]
                        {
                            new PackagedDeploymentConfiguration
                                {
                                    PackageWithBundleIdentifier = null,
                                    DeploymentConfiguration = defaultDeploymentConfig,
                                },
                        }
                        .ToList();
            }
            else
            {
                return
                    packagedConfigs.Select(
                        _ =>
                        new PackagedDeploymentConfiguration
                            {
                                PackageWithBundleIdentifier = _.PackageWithBundleIdentifier,
                                DeploymentConfiguration =
                                    _.DeploymentConfiguration.ApplyDefaults(
                                        defaultDeploymentConfig),
                            }).ToList();
            }
        }

        /// <summary>
        /// Retrieves the initialization strategies matching the specified type.
        /// </summary>
        /// <typeparam name="T">Type of initialization strategy to look for.</typeparam>
        /// <param name="baseCollection">Base collection of packaged configurations to operate on.</param>
        /// <returns>Collection of initialization strategies matching the type specified.</returns>
        public static IReadOnlyCollection<T> GetInitializationStrategiesOf<T>(
            this IReadOnlyCollection<PackagedDeploymentConfiguration> baseCollection)
            where T : InitializationStrategyBase
        {
            var ret = baseCollection.SelectMany(_ => _.GetInitializationStrategiesOf<T>()).ToList();

            return ret;
        }

        /// <summary>
        /// Retrieves the initialization strategies matching the specified type.
        /// </summary>
        /// <typeparam name="T">Type of initialization strategy to look for.</typeparam>
        /// <param name="baseCollection">Base collection of packaged configurations to operate on.</param>
        /// <returns>Collection of initialization strategies matching the type specified.</returns>
        public static IReadOnlyCollection<T> GetInitializationStrategiesOf<T>(
            this IReadOnlyCollection<IHaveInitializationStrategies> baseCollection)
            where T : InitializationStrategyBase
        {
            var ret = baseCollection.SelectMany(_ => _.GetInitializationStrategiesOf<T>()).ToList();

            return ret;
        }

        /// <summary>
        /// Retrieves the items that contain an initialization strategy matching the specified type.
        /// </summary>
        /// <typeparam name="T">Type of initialization strategy to look for.</typeparam>
        /// <param name="baseCollection">Base collection of packaged configurations to operate on.</param>
        /// <returns>Filtered collection of where the initialization strategies match the type specified.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Passing through and needs constraining.")]
        public static IReadOnlyCollection<PackagedDeploymentConfiguration> WhereContainsInitializationStrategyOf<T>(
            this IReadOnlyCollection<PackagedDeploymentConfiguration> baseCollection)
            where T : InitializationStrategyBase
        {
            var ret = baseCollection.Where(_ => _.GetInitializationStrategiesOf<T>().Count > 0).ToList();

            return ret;
        }

        /// <summary>
        /// Overrides the deployment config in a collection of packaged configurations.
        /// </summary>
        /// <param name="baseCollection">Base collection of packaged configurations to operate on.</param>
        /// <param name="overrideConfig">Configuration to apply as an override.</param>
        /// <returns>New collection of packaged configurations with overrides applied.</returns>
        public static IReadOnlyCollection<PackagedDeploymentConfiguration>
            OverrideDeploymentConfig(
            this IReadOnlyCollection<PackagedDeploymentConfiguration> baseCollection,
            DeploymentConfiguration overrideConfig)
        {
            var ret =
                baseCollection.Select(
                    _ =>
                    new PackagedDeploymentConfiguration
                        {
                            DeploymentConfiguration = overrideConfig,
                            PackageWithBundleIdentifier = _.PackageWithBundleIdentifier,
                            ItsConfigOverrides = _.ItsConfigOverrides,
                            InitializationStrategies = _.InitializationStrategies,
                        }).ToList();
            return ret;
        }
    }
}
