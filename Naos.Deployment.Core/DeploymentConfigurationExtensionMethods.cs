// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationExtensionMethods.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    using OBeautifulCode.Validation.Recipes;

    using static System.FormattableString;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode", Justification = "Prefer this layout.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Like it this way.")]
        public static DeploymentConfiguration Flatten(this IReadOnlyCollection<DeploymentConfiguration> deploymentConfigs)
        {
            // Makes sure we don't have competing IncludeInstanceInitializationScript values
            if (
                deploymentConfigs.Any(
                    _ => (_.DeploymentStrategy ?? new DeploymentStrategy()).IncludeInstanceInitializationScript)
                && deploymentConfigs.Any(
                    _ => !(_.DeploymentStrategy ?? new DeploymentStrategy()).IncludeInstanceInitializationScript))
            {
                throw new ArgumentException("Cannot have competing IncludeInstanceInitializationScript values.");
            }

            // Makes sure we don't have competing RunSetupSteps values
            if (
                deploymentConfigs.Any(
                    _ => (_.DeploymentStrategy ?? new DeploymentStrategy()).RunSetupSteps)
                && deploymentConfigs.Any(
                    _ => !(_.DeploymentStrategy ?? new DeploymentStrategy()).RunSetupSteps))
            {
                throw new ArgumentException("Cannot have competing RunSetupSteps values.");
            }

            // Makes sure we don't have competing TurnOffInstance values
            if (
                deploymentConfigs.Where(_ => _.PostDeploymentStrategy != null)
                    .Any(_ => _.PostDeploymentStrategy.TurnOffInstance)
                && deploymentConfigs.Where(_ => _.PostDeploymentStrategy != null)
                       .Any(_ => !_.PostDeploymentStrategy.TurnOffInstance))
            {
                throw new ArgumentException("Cannot have competing TurnOffInstance values.");
            }

            // since values are the same for the entire collection just take the first one...
            var deploymentStrategy = deploymentConfigs.First().DeploymentStrategy;

            // since values (where set) are the same for the entire collection just take the first one...
            var possiblePostDeploymentStrategy = deploymentConfigs.FirstOrDefault(_ => _.PostDeploymentStrategy != null);
            var postDeploymentStrategy = possiblePostDeploymentStrategy?.PostDeploymentStrategy;

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

            var allVolumes = deploymentConfigs.SelectMany(_ => _.Volumes ?? new List<Volume>()).ToList();
            var distinctDriveLetters = allVolumes.Select(_ => _.DriveLetter).Distinct().ToList();
            var volumes = new List<Volume>();
            foreach (var distinctDriveLetter in distinctDriveLetters)
            {
                var volumesForDriveLetter = allVolumes.Where(_ => _.DriveLetter == distinctDriveLetter).ToList();
                var sizeInGb = volumesForDriveLetter.Max(_ => _.SizeInGb);
                var distinctTypes = volumesForDriveLetter.Select(_ => _.Type).Distinct().ToList();

                var flattenedType = distinctTypes.Flatten();
                var volumeType = flattenedType;

                volumes.Add(
                    new Volume { DriveLetter = distinctDriveLetter, SizeInGb = sizeInGb, Type = volumeType });
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

            var tags = deploymentConfigs.SelectMany(_ => _.TagNameToValueMap ?? new Dictionary<string, string>()).ToDictionary(k => k.Key, v => v.Value);

            var operatingSystem = GetMoreInclusiveOperatingSystem(
                deploymentConfigs.Where(_ => _.InstanceType != null).Select(_ => _.InstanceType.OperatingSystem)
                    .Distinct().ToList());

            var ret = new DeploymentConfiguration
                          {
                              InstanceCount = instanceCount,
                              InstanceType =
                                  new InstanceType
                                      {
                                          SpecificInstanceTypeSystemId = deploymentConfigs.Select(_ => _.InstanceType?.SpecificInstanceTypeSystemId).Distinct().SingleOrDefault(_ => _ != null),
                                          SpecificImageSystemId = deploymentConfigs.Select(_ => _.InstanceType?.SpecificImageSystemId).Distinct().SingleOrDefault(_ => _ != null),
                                          RamInGb = deploymentConfigs.Max(_ => _.InstanceType == null ? 0 : _.InstanceType.RamInGb),
                                          VirtualCores = deploymentConfigs.Max(_ => _.InstanceType == null ? 0 : _.InstanceType.VirtualCores),
                                          OperatingSystem =
                                              operatingSystem,
                                      },
                              InstanceAccessibility = accessibilityToUse,
                              Volumes = volumes,
                              ChocolateyPackages = chocolateyPackagesToUse,
                              DeploymentStrategy = deploymentStrategy,
                              PostDeploymentStrategy = postDeploymentStrategy,
                              TagNameToValueMap = tags,
                          };

            if (!string.IsNullOrWhiteSpace(ret.InstanceType.SpecificImageSystemId) &&
                ((ret.InstanceType.OperatingSystem as OperatingSystemDescriptionWindows)?.Sku != WindowsSku.SpecificImageSupplied) &&
                ((ret.InstanceType.OperatingSystem as OperatingSystemDescriptionLinux)?.Distribution != LinuxDistribution.SpecificImageSupplied))
            {
                throw new ArgumentException(Invariant(
                    $"The flattened instance type has a SpecificImageSystemId: '{ret.InstanceType.SpecificImageSystemId}' but does not have the corresponding {nameof(WindowsSku)}: '{nameof(WindowsSku.SpecificImageSupplied)}' or {nameof(LinuxDistribution)}: '{nameof(LinuxDistribution.SpecificImageSupplied)}', instead it is: '{ret.InstanceType.OperatingSystem}'."));
            }

            return ret;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "VolumeType", Justification = "Spelling/name is correct.")]
        private static VolumeType Flatten(this IReadOnlyCollection<VolumeType> types)
        {
            if (types.Any(_ => _ == VolumeType.Instance) && types.Any(_ => _ != VolumeType.Instance))
            {
                throw new NotSupportedException("Cannot have flatten volumes that are Instance with those that are not.");
            }

            if (types.Contains(VolumeType.HighPerformance))
            {
                return VolumeType.HighPerformance;
            }
            else if (types.Contains(VolumeType.Standard))
            {
                return VolumeType.Standard;
            }
            else if (types.Contains(VolumeType.LowPerformance))
            {
                return VolumeType.LowPerformance;
            }
            else if (types.Contains(VolumeType.DoesNotMatter))
            {
                return VolumeType.DoesNotMatter;
            }
            else
            {
                throw new NotSupportedException("Unsupported " + nameof(VolumeType) + " in collection: " + string.Join(",", types));
            }
        }

        /// <summary>
        /// Creates a new config apply the provided config as an override.
        /// </summary>
        /// <param name="deploymentConfigInitial">Base deployment config to override.</param>
        /// <param name="deploymentConfigOverride">Overrides to apply.</param>
        /// <returns>New config with overrides applied.</returns>
        public static DeploymentConfiguration ApplyOverrides(
            this DeploymentConfiguration deploymentConfigInitial,
            DeploymentConfiguration deploymentConfigOverride)
        {
            if (deploymentConfigOverride == null)
            {
                return deploymentConfigInitial;
            }

            new { deploymentConfigInitial }.Must().NotBeNull();

            var instanceAccessibility = deploymentConfigInitial.InstanceAccessibility;
            var overrideAccessibility = deploymentConfigOverride.InstanceAccessibility;
            if (overrideAccessibility != InstanceAccessibility.DoesNotMatter)
            {
                instanceAccessibility = overrideAccessibility;
            }

            var overrideOperatingSystem = deploymentConfigOverride.InstanceType?.OperatingSystem;
            var initialOperatingSystem = deploymentConfigInitial.InstanceType?.OperatingSystem;
            if (initialOperatingSystem == null && overrideOperatingSystem == null)
            {
                throw new DeploymentException("Must specify operating system in either default or specific config.");
            }

            var operatingSystem = overrideOperatingSystem ?? initialOperatingSystem;

            var instanceCount = deploymentConfigOverride.InstanceCount == 0
                                    ? deploymentConfigInitial.InstanceCount
                                    : deploymentConfigOverride.InstanceCount;

            var ret = new DeploymentConfiguration()
                          {
                              InstanceCount = instanceCount,
                              InstanceAccessibility = instanceAccessibility,
                              InstanceType = deploymentConfigOverride.InstanceType ?? deploymentConfigInitial.InstanceType,
                              Volumes = deploymentConfigOverride.Volumes ?? deploymentConfigInitial.Volumes,
                              ChocolateyPackages = deploymentConfigOverride.ChocolateyPackages ?? deploymentConfigInitial.ChocolateyPackages,
                              DeploymentStrategy = deploymentConfigOverride.DeploymentStrategy ?? deploymentConfigInitial.DeploymentStrategy,
                              PostDeploymentStrategy = deploymentConfigOverride.PostDeploymentStrategy ?? deploymentConfigInitial.PostDeploymentStrategy,
                              TagNameToValueMap = deploymentConfigOverride.TagNameToValueMap ?? deploymentConfigInitial.TagNameToValueMap,
                          };

            if (ret.InstanceType == null)
            {
                ret.InstanceType = new InstanceType();
            }

            ret.InstanceType.OperatingSystem = operatingSystem;

            return ret;
        }

        /// <summary>
        /// Creates a copy with the null replaced by any provided default values (base level check only!).
        /// </summary>
        /// <param name="deploymentConfigInitial">Base deployment config to work with.</param>
        /// <param name="defaultDeploymentConfig">Defaults to use in case of a null value.</param>
        /// <returns>Copy of provided config with defaults applied.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Like it this way.")]
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

            var operatingSystem = deploymentConfigInitial?.InstanceType?.OperatingSystem ??
                                  defaultDeploymentConfig?.InstanceType?.OperatingSystem;

            // if the default isn't actually specifying anything then default to standard drive type (might wanna make configurable eventually...)
            var volumes = deploymentConfigInitial?.Volumes ?? defaultDeploymentConfig.Volumes;
            if (volumes != null)
            {
                foreach (var volume in volumes)
                {
                    if (volume.Type == VolumeType.DoesNotMatter)
                    {
                        volume.Type = VolumeType.Standard;
                    }
                }
            }

            var instanceCount = deploymentConfigInitial == null || deploymentConfigInitial.InstanceCount <= 0
                                    ? defaultDeploymentConfig.InstanceCount
                                    : deploymentConfigInitial.InstanceCount;

            if (instanceCount <= 0)
            {
                instanceCount = 1;
            }

            var ret = new DeploymentConfiguration
                          {
                              InstanceCount = instanceCount,
                              InstanceAccessibility = accessibilityValue,
                              InstanceType = deploymentConfigInitial?.InstanceType ?? defaultDeploymentConfig.InstanceType,
                              Volumes = volumes,
                              ChocolateyPackages = deploymentConfigInitial?.ChocolateyPackages ?? defaultDeploymentConfig.ChocolateyPackages,
                              DeploymentStrategy = deploymentConfigInitial?.DeploymentStrategy ?? defaultDeploymentConfig.DeploymentStrategy,
                              PostDeploymentStrategy = deploymentConfigInitial?.PostDeploymentStrategy ?? defaultDeploymentConfig.PostDeploymentStrategy,
                              TagNameToValueMap = deploymentConfigInitial?.TagNameToValueMap ?? defaultDeploymentConfig.TagNameToValueMap,
                          };

            if (ret.InstanceType != null)
            {
                ret.InstanceType.OperatingSystem = operatingSystem;
            }

            return ret;
        }

        private static OperatingSystemDescriptionBase GetMoreInclusiveOperatingSystem(IReadOnlyCollection<OperatingSystemDescriptionBase> operatingSystems)
        {
            if (operatingSystems == null || operatingSystems.Count == 0)
            {
                throw new DeploymentException("Must specify an operating system, check defaults if this is not expected.");
            }

            if (operatingSystems.Count == 1)
            {
                return operatingSystems.Single();
            }

            var distinctOperatingSystemTypes =
                operatingSystems.Select(_ => _.GetType()).Distinct().ToList();
            if (distinctOperatingSystemTypes.Count > 1)
            {
                throw new DeploymentException("Cannot deploy packages with differing requirements of operating system type.");
            }

            if (distinctOperatingSystemTypes.Single() == typeof(OperatingSystemDescriptionLinux))
            {
                var distributions = operatingSystems.Select(_ => (OperatingSystemDescriptionLinux)_).Select(_ => _.Distribution).ToList();
                if (distributions.Contains(LinuxDistribution.SpecificImageSupplied))
                {
                    return new OperatingSystemDescriptionLinux { Distribution = LinuxDistribution.SpecificImageSupplied };
                }
                else
                {
                    return new OperatingSystemDescriptionLinux { Distribution = LinuxDistribution.DoesNotMatter };
                }
            }

            if (distinctOperatingSystemTypes.Single() == typeof(OperatingSystemDescriptionWindows))
            {
                var windowsSkus = operatingSystems.Select(_ => (OperatingSystemDescriptionWindows)_).Select(_ => _.Sku).ToList();

                if (windowsSkus.Count == 1)
                {
                    return new OperatingSystemDescriptionWindows { Sku = windowsSkus.Single() };
                }

                if (windowsSkus.Contains(WindowsSku.SqlStandard))
                {
                    return new OperatingSystemDescriptionWindows { Sku = WindowsSku.SqlStandard };
                }

                if (windowsSkus.Contains(WindowsSku.SqlWeb))
                {
                    return new OperatingSystemDescriptionWindows { Sku = WindowsSku.SqlWeb };
                }

                if (windowsSkus.Contains(WindowsSku.Base))
                {
                    return new OperatingSystemDescriptionWindows { Sku = WindowsSku.Base };
                }

                if (windowsSkus.Contains(WindowsSku.Core))
                {
                    return new OperatingSystemDescriptionWindows { Sku = WindowsSku.Core };
                }

                throw new DeploymentException(
                    "Could not find the appropriate Windows SKU from the list (perhaps there is an unsupported type in there): "
                    + string.Join(",", windowsSkus));
            }

            var operatingSystemsStrings = operatingSystems.Select(_ => _.ToString()).ToList();
            var operatingSystemsAddIn = string.Join(",", operatingSystemsStrings);

            throw new DeploymentException("Could not determine the correct operating system to use from the list: " + operatingSystemsAddIn);
        }
    }
}
