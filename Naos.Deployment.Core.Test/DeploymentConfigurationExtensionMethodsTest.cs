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
                                           InstanceType = "t2.medium",
                                           IsPubliclyAccessible = true,
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
                                           InstanceType = "t2.medium",
                                           IsPubliclyAccessible = true,
                                       };

            var flattenedConfig = new[] { deploymentConfig }.Flatten();
            Assert.Equal(deploymentConfig.InstanceType, flattenedConfig.InstanceType);
        }

        [Fact]
        public static void Flatten_TwoConfigsConflictingPublicAccesiblity_Throws()
        {
            var deploymentConfigs = new[]
                                        {
                                            new DeploymentConfiguration()
                                                {
                                                    InstanceType = "t2.medium",
                                                    IsPubliclyAccessible = false,
                                                },
                                            new DeploymentConfiguration()
                                                {
                                                    InstanceType = "t2.medium",
                                                    IsPubliclyAccessible = true,
                                                },
                                        };

            var ex = Assert.Throws<DeploymentException>(() => deploymentConfigs.Flatten());
            Assert.Equal("Cannot deploy packages with requirements of public accessibly.", ex.Message);
        }

        [Fact]
        public static void ApplyDefaults_NullValues_BecomeDefaults()
        {
            var baseConfig = new DeploymentConfiguration();
            var defaultConfig = new DeploymentConfiguration()
                                    {
                                        InstanceType = "t2.medium",
                                        IsPubliclyAccessible = false,
                                        Volumes =
                                            new[]
                                                {
                                                    new Volume()
                                                        {
                                                            DriveLetter = "C",
                                                            SizeInGb = 50,
                                                        }
                                                }
                                    };

            var appliedConfig = baseConfig.ApplyDefaults(defaultConfig);
            Assert.Equal(defaultConfig.IsPubliclyAccessible, appliedConfig.IsPubliclyAccessible);
            Assert.Equal(defaultConfig.InstanceType, appliedConfig.InstanceType);
            Assert.Equal(1, appliedConfig.Volumes.Count);
            Assert.Equal(defaultConfig.Volumes.Single().DriveLetter, appliedConfig.Volumes.Single().DriveLetter);
            Assert.Equal(defaultConfig.Volumes.Single().SizeInGb, appliedConfig.Volumes.Single().SizeInGb);
        }
    }
}
