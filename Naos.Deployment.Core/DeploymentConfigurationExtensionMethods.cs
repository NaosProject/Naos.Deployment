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
                deploymentConfigs.Where(_ => _.InstanceAccessibility != InstanceAccessibility.DoesNotMatter)
                    .Select(_ => _.InstanceAccessibility)
                    .Distinct()
                    .ToList();

            if (distinctPubliclyAccessible.Count > 1)
            {
                throw new DeploymentException("Cannot deploy packages with differing requirements of accessibly.");
            }

            // if the distinct (minus 'DoesNotMatter') has a count of 1 then the first non-null/'DoesNotMatter' will be the value to use.
            var firstOrDefaultPubliclyAccessibility =
                deploymentConfigs.FirstOrDefault(_ => _.InstanceAccessibility != InstanceAccessibility.DoesNotMatter);
            var accessibilityToUse = firstOrDefaultPubliclyAccessibility == null
                                         ? InstanceAccessibility.Private
                                         : firstOrDefaultPubliclyAccessibility.InstanceAccessibility;

            if (deploymentConfigs.Count == 1)
            {
                var singleValueToReturn = deploymentConfigs.Single();
                singleValueToReturn.InstanceAccessibility = accessibilityToUse;
                return singleValueToReturn;
            }

            var allVolumes = deploymentConfigs.SelectMany(_ => _.Volumes ?? new List<Volume>()).ToList();
            var distinctDriveLetters = allVolumes.Select(_ => _.DriveLetter).Distinct().ToList();
            var volumes = new List<Volume>();
            foreach (var distinctDriveLetter in distinctDriveLetters)
            {
                var sizeInGb = allVolumes.Where(_ => _.DriveLetter == distinctDriveLetter).Max(_ => _.SizeInGb);
                volumes.Add(new Volume { DriveLetter = distinctDriveLetter, SizeInGb = sizeInGb });
            }

            var allChocolateyPackages =
                deploymentConfigs.SelectMany(_ => _.ChocolateyPackages ?? new List<PackageDescription>()).ToList();

            // thin out duplicates
            var distinctPackageStrings =
                allChocolateyPackages.Select(_ => _.GetIdDotVersionString()).Distinct().ToList();
            var chocolateyPackagesToUse = new List<PackageDescription>();
            foreach (var packageString in distinctPackageStrings)
            {
                chocolateyPackagesToUse.Add(
                    allChocolateyPackages.First(_ => _.GetIdDotVersionString() == packageString));
            }

            var instanceCount = deploymentConfigs.Max(_ => _.InstanceCount);

            var ret = new DeploymentConfiguration()
                          {
                              InstanceCount = instanceCount,
                              InstanceType = 
                                  new InstanceType
                                      {
                                          RamInGb =
                                              deploymentConfigs.Max(
                                                  _ => _.InstanceType == null ? 0 : _.InstanceType.RamInGb),
                                          VirtualCores =
                                              deploymentConfigs.Max(
                                                  _ =>
                                                  _.InstanceType == null ? 0 : _.InstanceType.VirtualCores),
                                          WindowsSku =
                                              GetLargestWindowsSku(
                                                deploymentConfigs
                                                    .Where(_ => _.InstanceType != null)
                                                    .Select(_ => _.InstanceType.WindowsSku)
                                                    .Distinct()
                                                    .ToList()),
                                      },
                              InstanceAccessibility = accessibilityToUse,
                              Volumes = volumes,
                              ChocolateyPackages = chocolateyPackagesToUse,
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

            var instanceAccessibility = deploymentConfigInitial.InstanceAccessibility;
            var overrideAccessibility = deploymentConfigOverride.InstanceAccessibility;
            if (overrideAccessibility != InstanceAccessibility.DoesNotMatter)
            {
                instanceAccessibility = overrideAccessibility;
            }

            var windowsSku = (deploymentConfigInitial.InstanceType ?? new InstanceType()).WindowsSku;
            var overrideWindowsSku = (deploymentConfigOverride.InstanceType ?? new InstanceType()).WindowsSku;
            if (overrideWindowsSku != WindowsSku.DoesNotMatter)
            {
                windowsSku = overrideWindowsSku;
            }

            var instanceCount = deploymentConfigOverride.InstanceCount == 0
                                    ? deploymentConfigInitial.InstanceCount
                                    : deploymentConfigOverride.InstanceCount;

            var ret = new DeploymentConfiguration()
                          {
                              InstanceCount = instanceCount,
                              InstanceAccessibility = instanceAccessibility,
                              InstanceType =
                                  deploymentConfigOverride.InstanceType
                                  ?? deploymentConfigInitial.InstanceType,
                              Volumes =
                                  deploymentConfigOverride.Volumes
                                  ?? deploymentConfigInitial.Volumes,
                              ChocolateyPackages =
                                  deploymentConfigOverride.ChocolateyPackages
                                  ?? deploymentConfigInitial.ChocolateyPackages,
                          };

            if (ret.InstanceType == null)
            {
                ret.InstanceType = new InstanceType();
            }

            ret.InstanceType.WindowsSku = windowsSku;

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

            var accessibilityValue = deploymentConfigInitial == null ? InstanceAccessibility.DoesNotMatter : deploymentConfigInitial.InstanceAccessibility;
            if (accessibilityValue == InstanceAccessibility.DoesNotMatter)
            {
                accessibilityValue = defaultDeploymentConfig.InstanceAccessibility;
            }

            // if the default isn't actually specifying anything then default private for safety
            if (accessibilityValue == InstanceAccessibility.DoesNotMatter)
            {
                accessibilityValue = InstanceAccessibility.Private;
            }

            var windowsSku = deploymentConfigInitial == null || deploymentConfigInitial.InstanceType == null
                                 ? WindowsSku.DoesNotMatter
                                 : deploymentConfigInitial.InstanceType.WindowsSku;

            if (windowsSku == WindowsSku.DoesNotMatter && defaultDeploymentConfig.InstanceType != null)
            {
                windowsSku = defaultDeploymentConfig.InstanceType.WindowsSku;
            }

            var instanceCount = deploymentConfigInitial == null || deploymentConfigInitial.InstanceCount <= 0
                                    ? defaultDeploymentConfig.InstanceCount
                                    : deploymentConfigInitial.InstanceCount;

            if (instanceCount <= 0)
            {
                instanceCount = 1;
            }

            var ret = new DeploymentConfiguration()
                          {
                              InstanceCount = instanceCount,
                              InstanceAccessibility = accessibilityValue,
                              InstanceType =
                                  (deploymentConfigInitial == null
                                       ? null
                                       : deploymentConfigInitial.InstanceType)
                                  ?? defaultDeploymentConfig.InstanceType,
                              Volumes =
                                  (deploymentConfigInitial == null
                                       ? null
                                       : deploymentConfigInitial.Volumes)
                                  ?? defaultDeploymentConfig.Volumes,
                              ChocolateyPackages =
                                  (deploymentConfigInitial == null
                                       ? null
                                       : deploymentConfigInitial.ChocolateyPackages)
                                  ?? defaultDeploymentConfig.ChocolateyPackages,
                          };

            if (ret.InstanceType != null)
            {
                ret.InstanceType.WindowsSku = windowsSku;
            }

            return ret;
        }

        private static WindowsSku GetLargestWindowsSku(ICollection<WindowsSku> windowsSkus)
        {
            if (windowsSkus == null || windowsSkus.Count == 0)
            {
                return WindowsSku.DoesNotMatter;
            }

            if (windowsSkus.Count == 1)
            {
                return windowsSkus.Single();
            }

            if (windowsSkus.Contains(WindowsSku.SqlStandard))
            {
                return WindowsSku.SqlStandard;
            }

            if (windowsSkus.Contains(WindowsSku.SqlWeb))
            {
                return WindowsSku.SqlWeb;
            }

            if (windowsSkus.Contains(WindowsSku.Base))
            {
                return WindowsSku.Base;
            }

            if (windowsSkus.Contains(WindowsSku.Core))
            {
                return WindowsSku.Core;
            }

            throw new DeploymentException(
                "Could not find the appropriate Windows SKU from the list (perhaps there is an unsupported type in there): "
                + string.Join(",", windowsSkus));
        }
    }
}
