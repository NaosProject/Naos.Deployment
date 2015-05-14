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
            // Make sure we don't have duplicate drive letter assignments.
            if (deploymentConfigs.Any(deploymentConfig => (deploymentConfig.Volumes ?? new Volume[0]).Select(_ => _.DriveLetter).Distinct().Count() != (deploymentConfig.Volumes ?? new Volume[0]).Count))
            {
                throw new ArgumentException("Can't have two volumes with the same drive letter.");
            }

            // Check if there are conflicting levels of accessibility
            var distinctPubliclyAccessible =
                deploymentConfigs.Where(
                    _ =>
                    _.InstanceAccessibility != null && _.InstanceAccessibility != InstanceAccessibility.DoesntMatter)
                    .Select(_ => _.InstanceAccessibility)
                    .Distinct()
                    .ToList();
            if (distinctPubliclyAccessible.Count > 1)
            {
                throw new DeploymentException("Cannot deploy packages with differing requirements of accessibly.");
            }

            // if the distinct (minus 'DoesntMatter') has a count of 1 then the first non-null/'DoesntMatter' will be the value to use.
            var firstOrDefaultPubliclyAccessibility =
                deploymentConfigs.FirstOrDefault(
                    _ =>
                    _.InstanceAccessibility != null && _.InstanceAccessibility != InstanceAccessibility.DoesntMatter);
            var accessibilityToUse = firstOrDefaultPubliclyAccessibility == null
                                         ? InstanceAccessibility.Private
                                         : firstOrDefaultPubliclyAccessibility.InstanceAccessibility;

            if (deploymentConfigs.Count == 1)
            {
                var singleValueToReturn = deploymentConfigs.Single();
                singleValueToReturn.InstanceAccessibility = accessibilityToUse;
                return singleValueToReturn;
            }

            var ret = new DeploymentConfiguration()
                          {
                              InstanceType =
                                  new InstanceType
                                      {
                                          RamInGb = deploymentConfigs.Max(_ => _.InstanceType.RamInGb),
                                          VirtualCores = deploymentConfigs.Max(_ => _.InstanceType.VirtualCores),
                                      },
                              InstanceAccessibility = accessibilityToUse,
                              InitializationStrategies =
                                  deploymentConfigs.SelectMany(
                                      _ => _.InitializationStrategies).ToList(),
                              Volumes = deploymentConfigs.SelectMany(_ => _.Volumes).ToList(),
                          };

            return ret;
        }

        /// <summary>
        /// Creates a new config apply the provided config as an override.
        /// </summary>
        /// <param name="deploymentConfigInitial">Base deployment config to override.</param>
        /// <param name="deploymentConfigOverride">Overrides to apply</param>
        /// <returns>New config with overrides applied.</returns>
        public static DeploymentConfiguration ApplyOverrides(
            this DeploymentConfiguration deploymentConfigInitial,
            DeploymentConfiguration deploymentConfigOverride)
        {
            if (deploymentConfigOverride == null)
            {
                return deploymentConfigInitial;
            }

            var accessibilityValue = deploymentConfigInitial.InstanceAccessibility;
            if (accessibilityValue == null || accessibilityValue == InstanceAccessibility.DoesntMatter)
            {
                accessibilityValue = deploymentConfigOverride.InstanceAccessibility;
            }

            var ret = new DeploymentConfiguration()
                          {
                              InstanceAccessibility = accessibilityValue,
                              InstanceType =
                                  deploymentConfigOverride.InstanceType
                                  ?? deploymentConfigInitial.InstanceType,
                              InitializationStrategies =
                                  deploymentConfigOverride.InitializationStrategies
                                  ?? deploymentConfigInitial.InitializationStrategies,
                              Volumes =
                                  deploymentConfigOverride.Volumes
                                  ?? deploymentConfigInitial.Volumes,
                              ChocolateyPackages =
                                  deploymentConfigOverride.ChocolateyPackages
                                  ?? deploymentConfigInitial.ChocolateyPackages,
                          };

            return ret;
        }

        /// <summary>
        /// Creates a copy with the null replaced by any provided default values (base level check only!).
        /// </summary>
        /// <param name="deploymentConfigInitial">Base deployment config to work with.</param>
        /// <param name="defaultDeploymentConfig">Defaults to use in case of a null value.</param>
        /// <returns>Copy of provided config with defaults applied.</returns>
        public static DeploymentConfiguration ApplyDefaults(
            this DeploymentConfiguration deploymentConfigInitial,
            DeploymentConfiguration defaultDeploymentConfig)
        {
            if (defaultDeploymentConfig == null)
            {
                throw new ArgumentException("Cannot apply defaults from a null definition", "defaultDeploymentConfig");
            }

            var accessibilityValue = deploymentConfigInitial.InstanceAccessibility;
            if (accessibilityValue == null || accessibilityValue == InstanceAccessibility.DoesntMatter)
            {
                accessibilityValue = defaultDeploymentConfig.InstanceAccessibility;
            }

            // if the default isn't actually specifying anything then default private for safety
            if (accessibilityValue == null || accessibilityValue == InstanceAccessibility.DoesntMatter)
            {
                accessibilityValue = InstanceAccessibility.Private;
            }

            var ret = new DeploymentConfiguration()
                          {
                              InstanceAccessibility = accessibilityValue,
                              InstanceType =
                                  deploymentConfigInitial.InstanceType
                                  ?? defaultDeploymentConfig.InstanceType,
                              InitializationStrategies =
                                  deploymentConfigInitial.InitializationStrategies
                                  ?? defaultDeploymentConfig.InitializationStrategies,
                              Volumes =
                                  deploymentConfigInitial.Volumes
                                  ?? defaultDeploymentConfig.Volumes,
                              ChocolateyPackages = 
                                  deploymentConfigInitial.ChocolateyPackages
                                  ?? defaultDeploymentConfig.ChocolateyPackages,
                          };

            return ret;
        }
    }
}
