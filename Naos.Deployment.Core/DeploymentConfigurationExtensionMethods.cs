// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationExtensionMethods.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Methods added to instance configuration objects.
    /// </summary>
    public static class DeploymentConfigurationExtensionMethods
    {
        /// <summary>
        /// Flatten to a single config that retains the most accommodating of each option.
        /// </summary>
        /// <param name="deploymentConfigs">Deployment configurations to operate against.</param>
        /// <returns>Constructed deployment configuration of most accommodating options.</returns>
        public static DeploymentConfiguration Flatten(this ICollection<DeploymentConfiguration> deploymentConfigs)
        {
            // Validations first...
            if (deploymentConfigs.Any(deploymentConfig => (deploymentConfig.Volumes ?? new Volume[0]).Select(_ => _.DriveLetter).Distinct().Count() != (deploymentConfig.Volumes ?? new Volume[0]).Count))
            {
                throw new ArgumentException("Can't have two volumes with the same drive letter.");
            }

            var distinctPubliclyAccessible =
                deploymentConfigs.Where(_ => _.IsPubliclyAccessible != null)
                    .Select(_ => _.IsPubliclyAccessible)
                    .Distinct()
                    .ToList();
            if (distinctPubliclyAccessible.Count > 1)
            {
                throw new DeploymentException("Cannot deploy packages with requirements of public accessibly.");
            }

            // there are only nulls and a single other value so now update all nulls to that value...
            var firstOrDefaultPubliclyAccesible = deploymentConfigs.FirstOrDefault(_ => _.IsPubliclyAccessible != null);
            var isPubliclyAccesibleValueToUse = firstOrDefaultPubliclyAccesible == null
                                                    ? false
                                                    : firstOrDefaultPubliclyAccesible.IsPubliclyAccessible;

            if (deploymentConfigs.Count == 1)
            {
                var singleValueToReturn = deploymentConfigs.Single();
                singleValueToReturn.IsPubliclyAccessible = isPubliclyAccesibleValueToUse;
                return singleValueToReturn;
            }

            var ret = new DeploymentConfiguration()
                          {
                              InstanceType = CloudInfrastructureManager.InferLargestInstanceType(deploymentConfigs.Select(_ => _.InstanceType).ToList()),
                              IsPubliclyAccessible = isPubliclyAccesibleValueToUse,
                              InitializationStrategies = deploymentConfigs.SelectMany(_ => _.InitializationStrategies).ToList(),
                              Volumes = deploymentConfigs.SelectMany(_ => _.Volumes).ToList(),
                          };

            return ret;
        }

        /// <summary>
        /// Creates a new config apply the provided config as an override.
        /// </summary>
        /// <param name="deploymentConfigBase">Base deployment config to override.</param>
        /// <param name="deploymentConfigOverride">Overrides to apply</param>
        /// <returns>New config with overrides applied.</returns>
        public static DeploymentConfiguration ApplyOverrides(
            this DeploymentConfiguration deploymentConfigBase,
            DeploymentConfiguration deploymentConfigOverride)
        {
            if (deploymentConfigOverride == null)
            {
                return deploymentConfigBase;
            }

            var ret = new DeploymentConfiguration()
                          {
                              IsPubliclyAccessible =
                                  deploymentConfigOverride.IsPubliclyAccessible
                                  ?? deploymentConfigBase.IsPubliclyAccessible ?? false,
                              InstanceType =
                                  deploymentConfigOverride.InstanceType
                                  ?? deploymentConfigBase.InstanceType,
                              InitializationStrategies =
                                  deploymentConfigOverride.InitializationStrategies
                                  ?? deploymentConfigBase.InitializationStrategies,
                              Volumes =
                                  deploymentConfigOverride.Volumes
                                  ?? deploymentConfigBase.Volumes,
                          };

            return ret;
        }

        /// <summary>
        /// Creates a copy with the null replaced by any provided default values (base level check only!).
        /// </summary>
        /// <param name="deploymentConfigBase">Base deployment config to work with.</param>
        /// <param name="defaultDeploymentConfig">Defaults to use in case of a null value.</param>
        /// <returns>Copy of provided config with defaults applied.</returns>
        public static DeploymentConfiguration ApplyDefaults(
            this DeploymentConfiguration deploymentConfigBase,
            DeploymentConfiguration defaultDeploymentConfig)
        {
            if (defaultDeploymentConfig == null)
            {
                throw new ArgumentException("Cannot apply defaults from a null definition", "defaultDeploymentConfig");
            }

            var ret = new DeploymentConfiguration()
                          {
                              IsPubliclyAccessible =
                                  deploymentConfigBase.IsPubliclyAccessible
                                  ?? defaultDeploymentConfig.IsPubliclyAccessible ?? false,
                              InstanceType =
                                  deploymentConfigBase.InstanceType
                                  ?? defaultDeploymentConfig.InstanceType,
                              InitializationStrategies =
                                  deploymentConfigBase.InitializationStrategies
                                  ?? defaultDeploymentConfig.InitializationStrategies,
                              Volumes =
                                  deploymentConfigBase.Volumes
                                  ?? defaultDeploymentConfig.Volumes,
                          };

            return ret;
        }
    }
}
