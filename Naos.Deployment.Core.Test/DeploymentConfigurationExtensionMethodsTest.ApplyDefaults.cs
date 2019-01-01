// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationExtensionMethodsTest.ApplyDefaults.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    using Xunit;

    public static partial class DeploymentConfigurationExtensionMethodsTest
    {
        [Fact]
        public static void ApplyDefaults_InstanceCount_ZeroOverrideBecomesOne()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration() { InstanceCount = 0 };

            var config = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(1, config.InstanceCount);
        }

        [Fact]
        public static void ApplyDefaults_InstanceCount_NegativeOverrideBecomesOne()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration() { InstanceCount = -1 };

            var config = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(1, config.InstanceCount);
        }

        [Fact]
        public static void ApplyDefaults_NullValues_BecomeDefaults()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration()
                                    {
                                        InstanceType = new InstanceType
                                        {
                                            VirtualCores = 2,
                                            RamInGb = 4,
                                            OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.SqlStandard },
                                        },
                                        InstanceAccessibility = InstanceAccessibility.Private,
                                        Volumes =
                                            new[]
                                                {
                                                    new Volume()
                                                        {
                                                            DriveLetter = "C",
                                                            SizeInGb = 50,
                                                            Type = VolumeType.HighPerformance,
                                                        },
                                                },
                                        ChocolateyPackages = new[] { new PackageDescription { Id = "Chrome" } },
                                        DeploymentStrategy = new DeploymentStrategy { IncludeInstanceInitializationScript = true, RunSetupSteps = true },
                                        PostDeploymentStrategy = new PostDeploymentStrategy { TurnOffInstance = true },
                                        TagNameToValueMap = new Dictionary<string, string> { { "hello", "world" } },
            };

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(defaultConfig.InstanceAccessibility, appliedConfig.InstanceAccessibility);
            Assert.Equal(defaultConfig.InstanceType, appliedConfig.InstanceType);
            Assert.Equal(1, appliedConfig.Volumes.Count);
            Assert.Equal(defaultConfig.Volumes.Single().DriveLetter, appliedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(defaultConfig.Volumes.Single().SizeInGb, appliedConfig.Volumes.Single().SizeInGb);
            Assert.Equal(defaultConfig.Volumes.Single().Type, appliedConfig.Volumes.Single().Type);
            Assert.Equal(defaultConfig.ChocolateyPackages.Single().Id, appliedConfig.ChocolateyPackages.Single().Id);
            Assert.Equal(WindowsSku.SqlStandard, (appliedConfig.InstanceType.OperatingSystem as OperatingSystemDescriptionWindows)?.Sku);
            Assert.Equal(true, appliedConfig.DeploymentStrategy.IncludeInstanceInitializationScript);
            Assert.Equal(true, appliedConfig.DeploymentStrategy.RunSetupSteps);
            Assert.Equal(true, appliedConfig.PostDeploymentStrategy.TurnOffInstance);
            Assert.Equal(defaultConfig.TagNameToValueMap.Single(), appliedConfig.TagNameToValueMap.Single());
        }

        [Fact]
        public static void ApplyDefaults_DefaultAccessibleIsDefault_BecomesPrivate()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration();

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(InstanceAccessibility.Private, appliedConfig.InstanceAccessibility);
        }

        [Fact]
        public static void ApplyDefaults_DefaultAccessibleIsDoesNotMatter_BecomesPrivate()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration()
                                    {
                                        InstanceAccessibility = InstanceAccessibility.DoesNotMatter,
                                    };

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(InstanceAccessibility.Private, appliedConfig.InstanceAccessibility);
        }

        [Fact]
        public static void ApplyDefaults_DefaultVolumeTypeIsDefault_BecomesStandard()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration()
            {
                Volumes = new[] { new Volume() },
            };

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(VolumeType.Standard, appliedConfig.Volumes.Single().Type);
        }

        [Fact]
        public static void ApplyDefaults_DefaultVolumeTypeIsDoesNotMatter_BecomesStandard()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration()
                                    {
                                        Volumes = new[] { new Volume { Type = VolumeType.DoesNotMatter } },
                                    };

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(VolumeType.Standard, appliedConfig.Volumes.Single().Type);
        }
    }
}
