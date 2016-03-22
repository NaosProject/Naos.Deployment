// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationExtensionMethodsTest.ApplyOverrides.cs" company="Naos">
//   Copyright 2015 Naos
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

    public partial class DeploymentConfigurationExtensionMethodsTest
    {
        [Fact]
        public static void ApplyOverrides_InstanceCount_DefaultIsOverriden()
        {
            var baseConfig = new DeploymentConfiguration();
            var overrideConfig = new DeploymentConfiguration() { InstanceCount = 2 };
            var config = baseConfig.ApplyOverrides(overrideConfig);
            Assert.Equal(overrideConfig.InstanceCount, config.InstanceCount);
        }

        [Fact]
        public static void ApplyOverrides_NullOverridePropertiesAllowed_NothingOverwritten()
        {
            var baseConfig = new DeploymentConfiguration
            {
                InstanceAccessibility = InstanceAccessibility.Private,
                InstanceType = new InstanceType
                {
                    VirtualCores = 10,
                    RamInGb = 20,
                    WindowsSku = WindowsSku.SqlWeb,
                },
                Volumes = new[] { new Volume() { DriveLetter = "F", SizeInGb = 100 }, new Volume() { DriveLetter = "Q", SizeInGb = 1 } },
                ChocolateyPackages = new[] { new PackageDescription { Id = "Monkey" }, new PackageDescription { Id = "AnotherMonkey" } },
                DeploymentStrategy = new DeploymentStrategy { IncludeInstanceInitializationScript = true, RunSetupSteps = true },
                PostDeploymentStrategy = new PostDeploymentStrategy { TurnOffInstance = true }
            };

            var overrideConfig = new DeploymentConfiguration();

            var appliedConfig = baseConfig.ApplyOverrides(overrideConfig);
            Assert.Equal(baseConfig.InstanceAccessibility, appliedConfig.InstanceAccessibility);
            Assert.Equal(baseConfig.InstanceType.VirtualCores, appliedConfig.InstanceType.VirtualCores);
            Assert.Equal(baseConfig.InstanceType.RamInGb, appliedConfig.InstanceType.RamInGb);
            Assert.Equal(baseConfig.Volumes.First().DriveLetter, appliedConfig.Volumes.First().DriveLetter);
            Assert.Equal(baseConfig.Volumes.First().SizeInGb, appliedConfig.Volumes.First().SizeInGb);
            Assert.Equal(baseConfig.ChocolateyPackages.First().Id, appliedConfig.ChocolateyPackages.First().Id);
            Assert.Equal(baseConfig.InstanceType.WindowsSku, appliedConfig.InstanceType.WindowsSku);
            Assert.Equal(
                baseConfig.DeploymentStrategy.IncludeInstanceInitializationScript,
                appliedConfig.DeploymentStrategy.IncludeInstanceInitializationScript);
            Assert.Equal(baseConfig.DeploymentStrategy.RunSetupSteps, appliedConfig.DeploymentStrategy.RunSetupSteps);
            Assert.Equal(baseConfig.PostDeploymentStrategy.TurnOffInstance, appliedConfig.PostDeploymentStrategy.TurnOffInstance);
        }

        [Fact]
        public static void ApplyOverrides_NoPropertiesSet_EverythingOverwritten()
        {
            var baseConfig = new DeploymentConfiguration();

            var overrideConfig = new DeploymentConfiguration
                                    {
                                        InstanceCount = 4,
                                        InstanceAccessibility = InstanceAccessibility.Public,
                                        InstanceType = new InstanceType
                                                           {
                                                               VirtualCores = 4,
                                                               RamInGb = 10,
                                                               WindowsSku = WindowsSku.SqlStandard,
                                                           },
                                        Volumes = new[] { new Volume() { DriveLetter = "C", SizeInGb = 30 } },
                                        ChocolateyPackages = new[] { new PackageDescription { Id = "Chrome" } },
                                        DeploymentStrategy = new DeploymentStrategy { IncludeInstanceInitializationScript = true, RunSetupSteps = true },
                                        PostDeploymentStrategy = new PostDeploymentStrategy { TurnOffInstance = true }
                                    };

            var appliedConfig = baseConfig.ApplyOverrides(overrideConfig);
            Assert.Equal(overrideConfig.InstanceCount, appliedConfig.InstanceCount);
            Assert.Equal(overrideConfig.InstanceAccessibility, appliedConfig.InstanceAccessibility);
            Assert.Equal(overrideConfig.InstanceType.VirtualCores, appliedConfig.InstanceType.VirtualCores);
            Assert.Equal(overrideConfig.InstanceType.RamInGb, appliedConfig.InstanceType.RamInGb);
            Assert.Equal(overrideConfig.Volumes.Single().DriveLetter, appliedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(overrideConfig.Volumes.Single().SizeInGb, appliedConfig.Volumes.Single().SizeInGb);
            Assert.Equal(overrideConfig.ChocolateyPackages.Single().Id, appliedConfig.ChocolateyPackages.Single().Id);
            Assert.Equal(overrideConfig.InstanceType.WindowsSku, appliedConfig.InstanceType.WindowsSku);
            Assert.Equal(overrideConfig.DeploymentStrategy.IncludeInstanceInitializationScript, appliedConfig.DeploymentStrategy.IncludeInstanceInitializationScript);
            Assert.Equal(overrideConfig.DeploymentStrategy.RunSetupSteps, appliedConfig.DeploymentStrategy.RunSetupSteps);
            Assert.Equal(overrideConfig.PostDeploymentStrategy.TurnOffInstance, appliedConfig.PostDeploymentStrategy.TurnOffInstance);
        }

        [Fact]
        public static void ApplyOverrides_AllPropertiesSet_EverythingOverwritten()
        {
            var baseConfig = new DeploymentConfiguration
                                 {
                                     InstanceAccessibility = InstanceAccessibility.Private,
                                     InstanceType = new InstanceType
                                     {
                                         VirtualCores = 10,
                                         RamInGb = 20,
                                         WindowsSku = WindowsSku.SqlWeb,
                                     },
                                     Volumes = new[] { new Volume() { DriveLetter = "F", SizeInGb = 100, Type = VolumeType.LowPerformance }, new Volume() { DriveLetter = "Q", SizeInGb = 1, Type = VolumeType.LowPerformance } },
                                     ChocolateyPackages = new[] { new PackageDescription { Id = "Monkey" }, new PackageDescription { Id = "AnotherMonkey" } },
                                     DeploymentStrategy = new DeploymentStrategy { IncludeInstanceInitializationScript = true, RunSetupSteps = true },
                                     PostDeploymentStrategy = new PostDeploymentStrategy { TurnOffInstance = true }
                                 };

            var overrideConfig = new DeploymentConfiguration
                                    {
                                        InstanceAccessibility = InstanceAccessibility.Public,
                                        InstanceType = new InstanceType
                                                           {
                                                               VirtualCores = 4,
                                                               RamInGb = 10,
                                                               WindowsSku = WindowsSku.SqlStandard,
                                                           },
                                        Volumes = new[] { new Volume() { DriveLetter = "C", SizeInGb = 30, Type = VolumeType.HighPerformance } },
                                        ChocolateyPackages = new[] { new PackageDescription { Id = "Chrome" } },
                                        DeploymentStrategy = new DeploymentStrategy { IncludeInstanceInitializationScript = false, RunSetupSteps = false },
                                        PostDeploymentStrategy = new PostDeploymentStrategy { TurnOffInstance = false }
                                    };

            var appliedConfig = baseConfig.ApplyOverrides(overrideConfig);
            Assert.Equal(overrideConfig.InstanceAccessibility, appliedConfig.InstanceAccessibility);
            Assert.Equal(overrideConfig.InstanceType.VirtualCores, appliedConfig.InstanceType.VirtualCores);
            Assert.Equal(overrideConfig.InstanceType.RamInGb, appliedConfig.InstanceType.RamInGb);
            Assert.Equal(overrideConfig.Volumes.Single().DriveLetter, appliedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(overrideConfig.Volumes.Single().SizeInGb, appliedConfig.Volumes.Single().SizeInGb);
            Assert.Equal(overrideConfig.Volumes.Single().Type, appliedConfig.Volumes.Single().Type);
            Assert.Equal(overrideConfig.ChocolateyPackages.Single().Id, appliedConfig.ChocolateyPackages.Single().Id);
            Assert.Equal(overrideConfig.InstanceType.WindowsSku, appliedConfig.InstanceType.WindowsSku);
            Assert.Equal(overrideConfig.DeploymentStrategy.IncludeInstanceInitializationScript, appliedConfig.DeploymentStrategy.IncludeInstanceInitializationScript);
            Assert.Equal(overrideConfig.DeploymentStrategy.RunSetupSteps, appliedConfig.DeploymentStrategy.RunSetupSteps);
            Assert.Equal(overrideConfig.PostDeploymentStrategy.TurnOffInstance, appliedConfig.PostDeploymentStrategy.TurnOffInstance);
        }
    }
}
