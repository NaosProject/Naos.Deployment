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
        public static void Flatten_DifferentSkus_LargestWins()
        {
            Action<WindowsSku, WindowsSku> testSkuCombo = (smallerSku, largerSku) =>
                {
                    var deploymentConfigs = new[]
                                                {
                                                    new DeploymentConfiguration() { WindowsSku = smallerSku },
                                                    new DeploymentConfiguration() { WindowsSku = largerSku },
                                                };

                    var flattened = deploymentConfigs.Flatten();
                    Assert.Equal(largerSku, flattened.WindowsSku);
                };

            testSkuCombo(WindowsSku.Base, WindowsSku.Base);
            testSkuCombo(WindowsSku.Base, WindowsSku.SqlStandard);
            testSkuCombo(WindowsSku.Base, WindowsSku.SqlWeb);

            testSkuCombo(WindowsSku.SqlWeb, WindowsSku.SqlWeb);
            testSkuCombo(WindowsSku.SqlWeb, WindowsSku.SqlStandard);

            testSkuCombo(WindowsSku.SqlStandard, WindowsSku.SqlStandard);
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
                                        ChocolateyPackages = new[] { new PackageDescription { Id = "Chrome" } },
                                        WindowsSku = WindowsSku.SqlStandard,
                                    };

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(defaultConfig.InstanceAccessibility, appliedConfig.InstanceAccessibility);
            Assert.Equal(defaultConfig.InstanceType, appliedConfig.InstanceType);
            Assert.Equal(1, appliedConfig.Volumes.Count);
            Assert.Equal(defaultConfig.Volumes.Single().DriveLetter, appliedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(defaultConfig.Volumes.Single().SizeInGb, appliedConfig.Volumes.Single().SizeInGb);
            Assert.Equal(defaultConfig.ChocolateyPackages.Single().Id, appliedConfig.ChocolateyPackages.Single().Id);
            Assert.Equal(WindowsSku.SqlStandard, appliedConfig.WindowsSku);
        }

        [Fact]
        public static void ApplyDefaults_DefaultAccessibleIsNull_BecomesPrivate()
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
        public static void ApplyOverrides_EverythingOverwritten()
        {
            var baseConfig = new DeploymentConfiguration
                                 {
                                     InstanceAccessibility = InstanceAccessibility.Private,
                                     InstanceType = new InstanceType
                                     {
                                         VirtualCores = 10,
                                         RamInGb = 20,
                                     },
                                     Volumes = new[] { new Volume() { DriveLetter = "F", SizeInGb = 100 }, new Volume() { DriveLetter = "Q", SizeInGb = 1 } },
                                     ChocolateyPackages = new[] { new PackageDescription { Id = "Monkey" }, new PackageDescription { Id = "AnotherMonkey" } },
                                     WindowsSku = WindowsSku.SqlWeb,
                                 };

            var overrideConfig = new DeploymentConfiguration
                                    {
                                        InstanceAccessibility = InstanceAccessibility.Public,
                                        InstanceType = new InstanceType
                                                           {
                                                               VirtualCores = 4,
                                                               RamInGb = 10,
                                                           },
                                        Volumes = new[] { new Volume() { DriveLetter = "C", SizeInGb = 30 } },
                                        ChocolateyPackages = new[] { new PackageDescription { Id = "Chrome" } },
                                        WindowsSku = WindowsSku.SqlStandard,
                                    };

            var appliedConfig = baseConfig.ApplyOverrides(overrideConfig);
            Assert.Equal(overrideConfig.InstanceAccessibility, appliedConfig.InstanceAccessibility);
            Assert.Equal(overrideConfig.InstanceType.VirtualCores, appliedConfig.InstanceType.VirtualCores);
            Assert.Equal(overrideConfig.InstanceType.RamInGb, appliedConfig.InstanceType.RamInGb);
            Assert.Equal(overrideConfig.Volumes.Single().DriveLetter, appliedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(overrideConfig.Volumes.Single().SizeInGb, appliedConfig.Volumes.Single().SizeInGb);
            Assert.Equal(overrideConfig.ChocolateyPackages.Single().Id, appliedConfig.ChocolateyPackages.Single().Id);
            Assert.Equal(overrideConfig.WindowsSku, appliedConfig.WindowsSku);
        }
    }
}
