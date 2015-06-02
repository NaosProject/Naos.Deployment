// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagedDeploymentConfigurationExtensionMethods.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Additional behavior to add the initialization strategies.
    /// </summary>
    public static class PackagedDeploymentConfigurationExtensionMethods
    {
        public static ICollection<PackagedDeploymentConfiguration> ApplyDefaults(
            this ICollection<PackagedDeploymentConfiguration> packagedConfigs,
            DeploymentConfiguration defaultDeploymentConfig)
        {
            if (packagedConfigs.Count == 0)
            {
                return
                    new[]
                        {
                            new PackagedDeploymentConfiguration
                                {
                                    Package = null,
                                    DeploymentConfiguration = defaultDeploymentConfig
                                }
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
                                Package = _.Package,
                                DeploymentConfiguration =
                                    _.DeploymentConfiguration.ApplyDefaults(
                                        defaultDeploymentConfig)
                            }).ToList();
            }
        }

        /// <summary>
        /// Overrides the deployment config in a collection of packaged configurations.
        /// </summary>
        /// <param name="baseCollection">Base collection of packaged configurations to operate on.</param>
        /// <param name="overrideConfig">Configuration to apply as an override.</param>
        /// <returns>New collection of packaged configurations with overrides applied.</returns>
        public static ICollection<PackagedDeploymentConfiguration>
            OverrideDeploymentConfigAndMergeInitializationStrategies(
            this ICollection<PackagedDeploymentConfiguration> baseCollection,
            DeploymentConfiguration overrideConfig)
        {
            var ret = baseCollection.Select(
                _ =>
                new PackagedDeploymentConfiguration
                    {
                        DeploymentConfiguration =
                            new DeploymentConfiguration
                                {
                                    ChocolateyPackages =
                                        overrideConfig
                                        .ChocolateyPackages,
                                    InstanceAccessibility =
                                        overrideConfig
                                        .InstanceAccessibility,
                                    InstanceType =
                                        overrideConfig
                                        .InstanceType,
                                    Volumes =
                                        overrideConfig
                                        .Volumes,
                                    InitializationStrategies
                                        =
                                        overrideConfig
                                        .InitializationStrategies
                                        .Concat(
                                            _
                                        .DeploymentConfiguration
                                        .InitializationStrategies)
                                        .ToList(),
                                },
                        Package = _.Package
                    }).ToList();
            return
                ret;
        }
    }
}
