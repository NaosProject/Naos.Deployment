﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationExtensionMethodsTest.Flatten.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using FluentAssertions;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;
    using Xunit;

    public static partial class DeploymentConfigurationExtensionMethodsTest
    {
        [Fact]
        public static void Flatten_DifferentSpecificInstanceType_Throws()
        {
            var deploymentConfigOne = new DeploymentConfiguration() { InstanceType = new InstanceType { SpecificInstanceTypeSystemId = "server" } };
            var deploymentConfigTwo = new DeploymentConfiguration() { InstanceType = new InstanceType { SpecificInstanceTypeSystemId = "monkey" } };

            Action action = () => new[] { deploymentConfigOne, deploymentConfigTwo }.Flatten();
            action.ShouldThrow<InvalidOperationException>().WithMessage("Sequence contains more than one matching element");
        }

        [Fact]
        public static void Flatten_SameSpecificInstanceType_Uses()
        {
            var deploymentConfigOne = new DeploymentConfiguration() { InstanceType = new InstanceType { SpecificInstanceTypeSystemId = "server" } };
            var deploymentConfigTwo = new DeploymentConfiguration() { InstanceType = new InstanceType { SpecificInstanceTypeSystemId = "server" } };

            var config = new[] { deploymentConfigOne, deploymentConfigTwo }.Flatten();
            Assert.Equal(deploymentConfigTwo.InstanceType.SpecificInstanceTypeSystemId, config.InstanceType.SpecificInstanceTypeSystemId);
        }

        [Fact]
        public static void Flatten_DifferentSpecificImage_Throws()
        {
            var deploymentConfigOne = new DeploymentConfiguration() { InstanceType = new InstanceType { SpecificImageSystemId = "server" } };
            var deploymentConfigTwo = new DeploymentConfiguration() { InstanceType = new InstanceType { SpecificImageSystemId = "monkey" } };

            Action action = () => new[] { deploymentConfigOne, deploymentConfigTwo }.Flatten();
            action.ShouldThrow<InvalidOperationException>().WithMessage("Sequence contains more than one matching element");
        }

        [Fact]
        public static void Flatten_SameSpecificImage_Uses()
        {
            var deploymentConfigOne = new DeploymentConfiguration() { InstanceType = new InstanceType { SpecificImageSystemId = "server", OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.SpecificImageSupplied } } };
            var deploymentConfigTwo = new DeploymentConfiguration() { InstanceType = new InstanceType { SpecificImageSystemId = "server", OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.SpecificImageSupplied } } };

            var config = new[] { deploymentConfigOne, deploymentConfigTwo }.Flatten();
            Assert.Equal(deploymentConfigTwo.InstanceType.SpecificInstanceTypeSystemId, config.InstanceType.SpecificInstanceTypeSystemId);
        }

        [Fact]
        public static void Flatten_DifferentInstanceCount_TakesMax()
        {
            var deploymentConfigOne = new DeploymentConfiguration() { InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() }, InstanceCount = 1 };
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
                                        IncludeInstanceInitializationScript = true,
                                    },
                        };

            var b = new DeploymentConfiguration()
                        {
                            DeploymentStrategy =
                                new DeploymentStrategy
                                    {
                                        IncludeInstanceInitializationScript = false,
                                    },
                        };

            Action testCode = () => new[] { a, b }.Flatten();
            var ex = Assert.Throws<ArgumentException>(testCode);
            Assert.Equal("Cannot have competing IncludeInstanceInitializationScript values.", ex.Message);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sku", Justification = "Spelling/name is correct.")]
        public static void Flatten_SpecifiedAmiWithSpecificAmiSku_Windows_Throws()
        {
            var a = new DeploymentConfiguration()
                        {
                            InstanceType = new InstanceType { SpecificImageSystemId = "ami-something", OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.DoesNotMatter } },
                        };

            Action testCode = () => new[] { a }.Flatten();
            var ex = Assert.Throws<ArgumentException>(testCode);
            Assert.Equal("The flattened instance type has a SpecificImageSystemId: 'ami-something' but does not have the corresponding WindowsSku: 'SpecificImageSupplied' or LinuxDistribution: 'SpecificImageSupplied', instead it is: 'WindowsSku-DoesNotMatter'.", ex.Message);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sku", Justification = "Spelling/name is correct.")]
        public static void Flatten_SpecifiedAmiWithSpecificAmiSku_Linux_Throws()
        {
            var a = new DeploymentConfiguration()
                        {
                            InstanceType = new InstanceType { SpecificImageSystemId = "ami-something", OperatingSystem = new OperatingSystemDescriptionLinux { Distribution = LinuxDistribution.DoesNotMatter } },
                        };

            Action testCode = () => new[] { a }.Flatten();
            var ex = Assert.Throws<ArgumentException>(testCode);
            Assert.Equal("The flattened instance type has a SpecificImageSystemId: 'ami-something' but does not have the corresponding WindowsSku: 'SpecificImageSupplied' or LinuxDistribution: 'SpecificImageSupplied', instead it is: 'LinuxDistribution-DoesNotMatter'.", ex.Message);
        }

        [Fact]
        public static void Flatten_ConflictingDeploymentStrategyRunSetup_Throws()
        {
            var a = new DeploymentConfiguration()
                        {
                            DeploymentStrategy = new DeploymentStrategy { RunSetupSteps = true },
                        };

            var b = new DeploymentConfiguration()
                        {
                            DeploymentStrategy =
                                new DeploymentStrategy { RunSetupSteps = false },
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
                InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() },
                DeploymentStrategy =
                    new DeploymentStrategy
                    {
                        RunSetupSteps = true,
                        IncludeInstanceInitializationScript = true,
                    },
            };

            var b = new DeploymentConfiguration()
                        {
                            DeploymentStrategy =
                                new DeploymentStrategy
                                    {
                                        RunSetupSteps = true,
                                        IncludeInstanceInitializationScript = true,
                                    },
                        };

            var output = new[] { a, b }.Flatten();
            Assert.True(output.DeploymentStrategy.IncludeInstanceInitializationScript);
            Assert.True(output.DeploymentStrategy.RunSetupSteps);
        }

        [Fact]
        public static void Flatten_Tags_Merged()
        {
            var a = new DeploymentConfiguration()
            {
                InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() },
                TagNameToValueMap = new Dictionary<string, string> { { "hello", "world" } },
            };

            var b = new DeploymentConfiguration() { TagNameToValueMap = new Dictionary<string, string> { { "world", "hello" } }, };

            var output = new[] { a, b }.Flatten();
            Assert.Equal(2, output.TagNameToValueMap.Count);
            Assert.Equal(a.TagNameToValueMap.Single().Value, output.TagNameToValueMap[a.TagNameToValueMap.Single().Key]);
            Assert.Equal(b.TagNameToValueMap.Single().Value, output.TagNameToValueMap[b.TagNameToValueMap.Single().Key]);
        }

        [Fact]
        public static void Flatten_PostDeploymentStrategy_Persisted()
        {
            var a = new DeploymentConfiguration()
                        {
                            InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() },
                            PostDeploymentStrategy =
                                new PostDeploymentStrategy
                                    {
                                        TurnOffInstance = true,
                                    },
                        };

            var b = new DeploymentConfiguration()
                        {
                            PostDeploymentStrategy =
                                new PostDeploymentStrategy
                                    {
                                        TurnOffInstance = true,
                                    },
                        };

            var output = new[] { a, b }.Flatten();
            Assert.True(output.PostDeploymentStrategy.TurnOffInstance);
            Assert.True(output.PostDeploymentStrategy.TurnOffInstance);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TurnOff", Justification = "Spelling/name is correct.")]
        [Fact]
        public static void Flatten_PostDeploymentStrategy_ConflictingTurnOff_Throws()
        {
            var a = new DeploymentConfiguration()
                        {
                            PostDeploymentStrategy =
                                new PostDeploymentStrategy
                                    {
                                        TurnOffInstance = true,
                                    },
                        };

            var b = new DeploymentConfiguration()
                        {
                            PostDeploymentStrategy =
                                new PostDeploymentStrategy
                                    {
                                        TurnOffInstance = false,
                                    },
                        };

            Action testCode = () => new[] { a, b }.Flatten();
            var ex = Assert.Throws<ArgumentException>(testCode);
            Assert.Equal("Cannot have competing TurnOffInstance values.", ex.Message);
        }

        [Fact]
        public static void Flatten_PostDeploymentStrategy_MissingPostDeployment_IsNotConflictingTakesSetValue()
        {
            var a = new DeploymentConfiguration()
                        {
                            InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() },
                            PostDeploymentStrategy =
                                new PostDeploymentStrategy
                                    {
                                        TurnOffInstance = true,
                                    },
                        };

            var b = new DeploymentConfiguration() { PostDeploymentStrategy = null };

            var flattened = new[] { a, b }.Flatten();
            Assert.True(flattened.PostDeploymentStrategy.TurnOffInstance);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        public static void Flatten_TwoConfigsSameDriveLetter_OneVolumeSizeIsLargest()
        {
            var first = new DeploymentConfiguration()
                            {
                                InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() },
                                Volumes = new[] { new Volume { DriveLetter = "C", SizeInGb = 100 } },
                            };

            var second = new DeploymentConfiguration()
                             {
                                 Volumes = new[] { new Volume { DriveLetter = "C", SizeInGb = 50 } },
                             };

            var flattenedConfig = new[] { first, second }.Flatten();
            Assert.Equal(1, flattenedConfig.Volumes.Count);
            Assert.Equal("C", flattenedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(100, flattenedConfig.Volumes.Single().SizeInGb);
        }

        [Fact]
        public static void Flatten_TwoVolumesSameDriveLetter_TypeIsPersisted()
        {
            var first = new DeploymentConfiguration()
                            {
                                InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() },
                                Volumes = new[] { new Volume { DriveLetter = "C", SizeInGb = 100, Type = VolumeType.HighPerformance } },
                            };

            var second = new DeploymentConfiguration()
                             {
                                 Volumes = new[] { new Volume { DriveLetter = "C", SizeInGb = 50, Type = VolumeType.HighPerformance } },
                             };

            var flattenedConfig = new[] { first, second }.Flatten();
            Assert.Equal(1, flattenedConfig.Volumes.Count);
            Assert.Equal("C", flattenedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(100, flattenedConfig.Volumes.Single().SizeInGb);
            Assert.Equal(VolumeType.HighPerformance, flattenedConfig.Volumes.Single().Type);
        }

        [Fact]
        public static void Flatten_TwoVolumesSameDriveLetter_DoesNotMatterChangedToStandard()
        {
            var first = new DeploymentConfiguration()
                            {
                                InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() },
                                Volumes = new[] { new Volume { DriveLetter = "C", SizeInGb = 100 } },
                            };

            var second = new DeploymentConfiguration()
                             {
                                 Volumes = new[] { new Volume { DriveLetter = "C", SizeInGb = 50, Type = VolumeType.Standard } },
                             };

            var flattenedConfig = new[] { first, second }.Flatten();
            Assert.Equal(1, flattenedConfig.Volumes.Count);
            Assert.Equal("C", flattenedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(100, flattenedConfig.Volumes.Single().SizeInGb);
            Assert.Equal(VolumeType.Standard, flattenedConfig.Volumes.Single().Type);
        }

        [Fact]
        public static void Flatten_ConflictingVolumeTypes_HigherPerformanceWins()
        {
            var configReducer = new List<DeploymentConfiguration>();
            foreach (var option in Enum.GetValues(typeof(VolumeType)))
            {
                var typed = (VolumeType)option;

                // can't flatten Instances with others.
                if (typed == VolumeType.Instance)
                {
                    continue;
                }

                var config = new DeploymentConfiguration()
                {
                    InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() },
                    Volumes = new[] { new Volume { DriveLetter = "C", SizeInGb = 100, Type = typed } },
                };

                configReducer.Add(config);
            }

            // has all - high is expected
            var highActual = configReducer.Flatten();
            Assert.Equal(VolumeType.HighPerformance, highActual.Volumes.Single().Type);

            // all without high - standard is expected
            configReducer.RemoveAll(_ => _.Volumes.Single().Type == VolumeType.HighPerformance);
            var standardActual = configReducer.Flatten();
            Assert.Equal(VolumeType.Standard, standardActual.Volumes.Single().Type);

            // all without high, standard - low is expected
            configReducer.RemoveAll(_ => _.Volumes.Single().Type == VolumeType.Standard);
            var lowActual = configReducer.Flatten();
            Assert.Equal(VolumeType.LowPerformance, lowActual.Volumes.Single().Type);

            // all without high, standard, - DoesNotMatter is expected
            configReducer.RemoveAll(_ => _.Volumes.Single().Type == VolumeType.LowPerformance);
            var doesNotMatterActual = configReducer.Flatten();
            Assert.Equal(VolumeType.DoesNotMatter, doesNotMatterActual.Volumes.Single().Type);

            configReducer.RemoveAll(_ => _.Volumes.Single().Type == VolumeType.DoesNotMatter);
            Assert.Empty(configReducer);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        public static void Flatten_TwoConfigsConflictingAccessibility_Throws()
        {
            var deploymentConfigs = new[]
                                        {
                                            new DeploymentConfiguration()
                                                {
                                                    InstanceType =
                                                        new InstanceType
                                                            {
                                                                VirtualCores = 2,
                                                                RamInGb = 4,
                                                            },
                                                    InstanceAccessibility = InstanceAccessibility.Public,
                                                },
                                            new DeploymentConfiguration()
                                                {
                                                    InstanceType =
                                                        new InstanceType
                                                            {
                                                                VirtualCores = 2,
                                                                RamInGb = 4,
                                                            },
                                                    InstanceAccessibility = InstanceAccessibility.Private,
                                                },
                                        };

            var ex = Assert.Throws<DeploymentException>(() => deploymentConfigs.Flatten());
            Assert.Equal("Cannot deploy packages with differing requirements of accessibly.", ex.Message);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        public static void Flatten_TwoConfigsConflictingOperatingSystemType_Throws()
        {
            var deploymentConfigs = new[]
                                        {
                                            new DeploymentConfiguration()
                                                {
                                                    InstanceType =
                                                        new InstanceType
                                                            {
                                                                VirtualCores = 2,
                                                                RamInGb = 4,
                                                                OperatingSystem = new OperatingSystemDescriptionWindows(),
                                                            },
                                                    InstanceAccessibility = InstanceAccessibility.Public,
                                                },
                                            new DeploymentConfiguration()
                                                {
                                                    InstanceType =
                                                        new InstanceType
                                                            {
                                                                VirtualCores = 2,
                                                                RamInGb = 4,
                                                                OperatingSystem = new OperatingSystemDescriptionLinux(),
                                                            },
                                                    InstanceAccessibility = InstanceAccessibility.Private,
                                                },
                                        };

            var ex = Assert.Throws<DeploymentException>(() => deploymentConfigs.Flatten());
            Assert.Equal("Cannot deploy packages with differing requirements of accessibly.", ex.Message);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Skus", Justification = "Spelling/name is correct.")]
        public static void Flatten_DifferentSkus_LargestWins()
        {
            Action<WindowsSku, WindowsSku> testSkuCombo = (smallerSku, largerSku) =>
                {
                    var deploymentConfigs = new[]
                                                {
                                                    new DeploymentConfiguration() { InstanceType = new InstanceType() { OperatingSystem = new OperatingSystemDescriptionWindows { Sku = smallerSku } } },
                                                    new DeploymentConfiguration() { InstanceType = new InstanceType() { OperatingSystem = new OperatingSystemDescriptionWindows { Sku = largerSku } } },
                                                };

                    var flattened = deploymentConfigs.Flatten();
                    Assert.Equal(largerSku, (flattened.InstanceType.OperatingSystem as OperatingSystemDescriptionWindows)?.Sku);
                };

            testSkuCombo(WindowsSku.Base, WindowsSku.Base);
            testSkuCombo(WindowsSku.Base, WindowsSku.SqlStandard);
            testSkuCombo(WindowsSku.Base, WindowsSku.SqlWeb);

            testSkuCombo(WindowsSku.SqlWeb, WindowsSku.SqlWeb);
            testSkuCombo(WindowsSku.SqlWeb, WindowsSku.SqlStandard);

            testSkuCombo(WindowsSku.SqlStandard, WindowsSku.SqlStandard);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Chocolatey", Justification = "Spelling/name is correct.")]
        public static void Flatten_MultipleChocolateyPackages_MergedDistinctly()
        {
            var deploymentConfigs = new[]
                                        {
                                            new DeploymentConfiguration()
                                                {
                                                    InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows() },
                                                    ChocolateyPackages =
                                                        new[]
                                                            {
                                                                new PackageDescription() { Id = "Monkeys" },
                                                                new PackageDescription() { Id = "PandaBears" },
                                                            },
                                                },
                                            new DeploymentConfiguration()
                                                {
                                                    ChocolateyPackages =
                                                        new[]
                                                            {
                                                                new PackageDescription() { Id = "PandaBears" },
                                                            },
                                                },
                                        };

            var flattened = deploymentConfigs.Flatten();
            Assert.Equal(2, flattened.ChocolateyPackages.Count);
            Assert.Equal("Monkeys", flattened.ChocolateyPackages.First().Id);
            Assert.Equal("PandaBears", flattened.ChocolateyPackages.Skip(1).First().Id);
        }
    }
}
