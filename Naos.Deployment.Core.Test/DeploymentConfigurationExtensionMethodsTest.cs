// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationExtensionMethodsTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Linq;

    using Naos.Deployment.Contract;

    using Xunit;

    public class DeploymentConfigurationExtensionMethodsTest
    {
        [Fact]
        public static void Flatten_DuplicateDriveLetter_Throws()
        {
            var deploymentConfig = new DeploymentConfiguration()
                                       {
                                           InstanceType = new InstanceType { VirtualCores = 2, RamInGb = 4 },
                                           InstanceAccessibility = InstanceAccessibility.Public,
                                           Volumes =
                                               new[]
                                                   {
                                                       new Volume() { DriveLetter = "C" },
                                                       new Volume() { DriveLetter = "C" },
                                                   },
                                       };

            Action testCode = () => new[] { deploymentConfig }.Flatten();
            var ex = Assert.Throws<ArgumentException>(testCode);
            Assert.Equal("Can't have two volumes with the same drive letter.", ex.Message);
        }

        [Fact]
        public static void Flatten_SingleConfig_SameReturned()
        {
            var deploymentConfig = new DeploymentConfiguration()
                                       {
                                           InstanceType = new InstanceType { VirtualCores = 2, RamInGb = 4 },
                                           InstanceAccessibility = InstanceAccessibility.Public,
                                       };

            var flattenedConfig = new[] { deploymentConfig }.Flatten();
            Assert.Equal(deploymentConfig.InstanceType, flattenedConfig.InstanceType);
        }

        [Fact]
        public static void Flatten_TwoConfigsConflictingAccesiblity_Throws()
        {
            var deploymentConfigs = new[]
                                        {
                                            new DeploymentConfiguration()
                                                {
                                                    InstanceType =
                                                        new InstanceType
                                                            {
                                                                VirtualCores = 2,
                                                                RamInGb = 4
                                                            },
                                                    InstanceAccessibility = InstanceAccessibility.Public,
                                                },
                                            new DeploymentConfiguration()
                                                {
                                                    InstanceType =
                                                        new InstanceType
                                                            {
                                                                VirtualCores = 2,
                                                                RamInGb = 4
                                                            },
                                                    InstanceAccessibility = InstanceAccessibility.Private,
                                                },
                                        };

            var ex = Assert.Throws<DeploymentException>(() => deploymentConfigs.Flatten());
            Assert.Equal("Cannot deploy packages with differing requirements of accessibly.", ex.Message);
        }

        [Fact]
        public static void ApplyDefaults_NullValues_BecomeDefaults()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration()
                                    {
                                        InstanceType = new InstanceType { VirtualCores = 2, RamInGb = 4 },
                                        InstanceAccessibility = InstanceAccessibility.Private,
                                        Volumes =
                                            new[]
                                                {
                                                    new Volume()
                                                        {
                                                            DriveLetter = "C",
                                                            SizeInGb = 50,
                                                        }
                                                },
                                        InitializationStrategies = new[] { new InitializationStrategyWeb(),  },
                                        ChocolateyPackages = new[] { new PackageDescription { Id = "Chrome" } },
                                    };

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(defaultConfig.InstanceAccessibility, appliedConfig.InstanceAccessibility);
            Assert.Equal(defaultConfig.InstanceType, appliedConfig.InstanceType);
            Assert.Equal(1, appliedConfig.Volumes.Count);
            Assert.Equal(defaultConfig.Volumes.Single().DriveLetter, appliedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(defaultConfig.Volumes.Single().SizeInGb, appliedConfig.Volumes.Single().SizeInGb);
            Assert.Equal(defaultConfig.InitializationStrategies.Single().GetType(), appliedConfig.InitializationStrategies.Single().GetType());
            Assert.Equal(defaultConfig.ChocolateyPackages.Single().Id, appliedConfig.ChocolateyPackages.Single().Id);
        }

        [Fact]
        public static void ApplyDefaults_DefaultAccessibleIsNull_BecomesPrivate()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration()
                                    {
                                        InstanceAccessibility = null,
                                    };

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(InstanceAccessibility.Private, appliedConfig.InstanceAccessibility);
        }

        [Fact]
        public static void ApplyDefaults_DefaultAccessibleIsDoesntMatter_BecomesPrivate()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration()
                                    {
                                        InstanceAccessibility = InstanceAccessibility.DoesntMatter,
                                    };

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(InstanceAccessibility.Private, appliedConfig.InstanceAccessibility);
        }

        [Fact]
        public static void ApplyOverrides_EverythingOverwritten()
        {
            var baseConfig = new DeploymentConfiguration();
            var overrideConfig = new DeploymentConfiguration()
                                    {
                                        InstanceAccessibility = InstanceAccessibility.Public,
                                        InstanceType = new InstanceType
                                                           {
                                                               VirtualCores = 4,
                                                               RamInGb = 10,
                                                           },
                                        Volumes = new[] { new Volume() { DriveLetter = "C", SizeInGb = 30 } },
                                        InitializationStrategies = new[] { new InitializationStrategyWeb(),  },
                                        ChocolateyPackages = new[] { new PackageDescription { Id = "Chrome" } },
                                    };

            var appliedConfig = baseConfig.ApplyDefaults(overrideConfig);
            Assert.Equal(overrideConfig.InstanceAccessibility, appliedConfig.InstanceAccessibility);
            Assert.Equal(overrideConfig.InstanceType.VirtualCores, appliedConfig.InstanceType.VirtualCores);
            Assert.Equal(overrideConfig.InstanceType.RamInGb, appliedConfig.InstanceType.RamInGb);
            Assert.Equal(overrideConfig.Volumes.Single().DriveLetter, appliedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(overrideConfig.Volumes.Single().SizeInGb, appliedConfig.Volumes.Single().SizeInGb);
            Assert.Equal(overrideConfig.InitializationStrategies.Single().GetType(), appliedConfig.InitializationStrategies.Single().GetType());
            Assert.Equal(overrideConfig.ChocolateyPackages.Single().Id, appliedConfig.ChocolateyPackages.Single().Id);
        }
    }
}
