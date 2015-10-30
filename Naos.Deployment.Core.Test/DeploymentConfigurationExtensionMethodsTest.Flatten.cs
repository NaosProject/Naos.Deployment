// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationExtensionMethodsTest.Flatten.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Contract;

    using Xunit;

    public partial class DeploymentConfigurationExtensionMethodsTest
    {
        [Fact]
        public static void Flatten_DifferentInstanceCount_TakesMax()
        {
            var deploymentConfigOne = new DeploymentConfiguration() { InstanceCount = 1 };
            var deploymentConfigTwo = new DeploymentConfiguration() { InstanceCount = 2 };

            var config = new[] { deploymentConfigOne, deploymentConfigTwo }.Flatten();
            Assert.Equal(deploymentConfigTwo.InstanceCount, config.InstanceCount);
        }

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
        public static void Flatten_ConflictingDeploymentStrategyInitScript_Throws()
        {
            var a = new DeploymentConfiguration()
                        {
                            DeploymentStrategy =
                                new DeploymentStrategy
                                    {
                                        IncludeInstanceInitializationScript
                                            = true
                                    }
                        };

            var b = new DeploymentConfiguration()
                        {
                            DeploymentStrategy =
                                new DeploymentStrategy
                                    {
                                        IncludeInstanceInitializationScript
                                            = false
                                    }
                        };

            Action testCode = () => new[] { a, b }.Flatten();
            var ex = Assert.Throws<ArgumentException>(testCode);
            Assert.Equal("Cannot have competing IncludeInstanceInitializationScript values.", ex.Message);
        }

        [Fact]
        public static void Flatten_ConflictingDeploymentStrategyRunSetup_Throws()
        {
            var a = new DeploymentConfiguration()
                        {
                            DeploymentStrategy = new DeploymentStrategy { RunSetupSteps = true }
                        };

            var b = new DeploymentConfiguration()
                        {
                            DeploymentStrategy =
                                new DeploymentStrategy { RunSetupSteps = false }
                        };

            Action testCode = () => new[] { a, b }.Flatten();
            var ex = Assert.Throws<ArgumentException>(testCode);
            Assert.Equal("Cannot have competing RunSetupSteps values.", ex.Message);
        }

        [Fact]
        public static void Flatten_DeploymentStrategy_Persisted()
        {
            var a = new DeploymentConfiguration()
                        {
                            DeploymentStrategy =
                                new DeploymentStrategy
                                    {
                                        RunSetupSteps = true,
                                        IncludeInstanceInitializationScript =
                                            true
                                    }
                        };

            var b = new DeploymentConfiguration()
                        {
                            DeploymentStrategy =
                                new DeploymentStrategy
                                    {
                                        RunSetupSteps = true,
                                        IncludeInstanceInitializationScript =
                                            true
                                    }
                        };

            var output = new[] { a, b }.Flatten();
            Assert.Equal(true, output.DeploymentStrategy.IncludeInstanceInitializationScript);
            Assert.Equal(true, output.DeploymentStrategy.RunSetupSteps);
        }

        [Fact]
        public static void Flatten_PostDeploymentStrategy_Persisted()
        {
            var a = new DeploymentConfiguration()
                        {
                            PostDeploymentStrategy =
                                new PostDeploymentStrategy
                                    {
                                        TurnOffInstance = true
                                    }
                        };

            var b = new DeploymentConfiguration()
                        {
                            PostDeploymentStrategy =
                                new PostDeploymentStrategy
                                    {
                                        TurnOffInstance = true
                                    }
                        };

            var output = new[] { a, b }.Flatten();
            Assert.Equal(true, output.PostDeploymentStrategy.TurnOffInstance);
            Assert.Equal(true, output.PostDeploymentStrategy.TurnOffInstance);
        }

        [Fact]
        public static void Flatten_PostDeploymentStrategy_ConflictingTurnOff_Throws()
        {
            var a = new DeploymentConfiguration()
                        {
                            PostDeploymentStrategy =
                                new PostDeploymentStrategy
                                    {
                                        TurnOffInstance = true
                                    }
                        };

            var b = new DeploymentConfiguration()
                        {
                            PostDeploymentStrategy =
                                new PostDeploymentStrategy
                                    {
                                        TurnOffInstance = false
                                    }
                        };

            Action testCode = () => new[] { a, b }.Flatten();
            var ex = Assert.Throws<ArgumentException>(testCode);
            Assert.Equal("Cannot have competing TurnOffInstance values.", ex.Message);
        }

        [Fact]
        public static void Flatten_TwoConfigsSameDriveLetter_OneVolumeSizeIsLargest()
        {
            var first = new DeploymentConfiguration()
                            {
                                Volumes =
                                    new[] { new Volume { DriveLetter = "C", SizeInGb = 100 } }
                            };

            var second = new DeploymentConfiguration()
                             {
                                 Volumes =
                                     new[] { new Volume { DriveLetter = "C", SizeInGb = 50 } }
                             };

            var flattenedConfig = new[] { first, second }.Flatten();
            Assert.Equal(1, flattenedConfig.Volumes.Count);
            Assert.Equal("C", flattenedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(100, flattenedConfig.Volumes.Single().SizeInGb);
        }

        [Fact]
        public static void Flatten_ConflictingVolumeTypes_Throws()
        {
            var a = new DeploymentConfiguration()
                            {
                                Volumes =
                                    new[] { new Volume { DriveLetter = "C", SizeInGb = 100, Type = VolumeType.HighPerformance } }
                            };

            var b = new DeploymentConfiguration()
                             {
                                 Volumes =
                                     new[] { new Volume { DriveLetter = "C", SizeInGb = 50, Type = VolumeType.LowPerformance } }
                             };

            Action testCode = () => new[] { a, b }.Flatten();
            var ex = Assert.Throws<ArgumentException>(testCode);
            Assert.Equal("Cannot have competing Volume Type values for the same drive letter: C", ex.Message);
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
                                                    new DeploymentConfiguration() { InstanceType = new InstanceType() { WindowsSku = smallerSku } },
                                                    new DeploymentConfiguration() { InstanceType = new InstanceType() { WindowsSku = largerSku } },
                                                };

                    var flattened = deploymentConfigs.Flatten();
                    Assert.Equal(largerSku, flattened.InstanceType.WindowsSku);
                };

            testSkuCombo(WindowsSku.Base, WindowsSku.Base);
            testSkuCombo(WindowsSku.Base, WindowsSku.SqlStandard);
            testSkuCombo(WindowsSku.Base, WindowsSku.SqlWeb);

            testSkuCombo(WindowsSku.SqlWeb, WindowsSku.SqlWeb);
            testSkuCombo(WindowsSku.SqlWeb, WindowsSku.SqlStandard);

            testSkuCombo(WindowsSku.SqlStandard, WindowsSku.SqlStandard);
        }

        [Fact]
        public static void Flatten_MultipleChocolateyPackages_MergedDistinctly()
        {
            var deploymentConfigs = new[]
                                        {
                                            new DeploymentConfiguration()
                                                {
                                                    ChocolateyPackages =
                                                        new[]
                                                            {
                                                                new PackageDescriptionWithOverrides() { Id = "Monkeys" },
                                                                new PackageDescriptionWithOverrides() { Id = "PandaBears" }
                                                            }
                                                },
                                            new DeploymentConfiguration()
                                                {
                                                    ChocolateyPackages =
                                                        new[]
                                                            {
                                                                new PackageDescriptionWithOverrides() { Id = "PandaBears" }
                                                            }
                                                },
                                        };

            var flattened = deploymentConfigs.Flatten();
            Assert.Equal(2, flattened.ChocolateyPackages.Count);
            Assert.Equal("Monkeys", flattened.ChocolateyPackages.First().Id);
            Assert.Equal("PandaBears", flattened.ChocolateyPackages.Skip(1).First().Id);
        }
    }
}
