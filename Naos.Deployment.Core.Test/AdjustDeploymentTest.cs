// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustDeploymentTest.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using FluentAssertions;

    using Naos.Configuration.Domain;
    using Naos.Cron;
    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;
    using Naos.Logging.Persistence;
    using Naos.MessageBus.Domain;
    using Naos.Packaging.Domain;
    using Naos.Packaging.NuGet;
    using Naos.Serialization.Factory;
    using Naos.Serialization.Json;
    using Xunit;

    public static class AdjustDeploymentTest
    {
        private static readonly byte[] MessageBusHarnessPackageBytes;

        private static readonly MessageBusHandlerHarnessConfiguration MessageBusHandlerHarnessConfiguration;

        private static readonly DatabaseManagementConfiguration DatabaseManagementConfiguration;

        private static readonly ConfigFileManager ConfigFileManager;

        private static readonly DeploymentConfiguration DeploymentConfig;

        private static readonly SetupStepFactorySettings StepFactorySettings =
            Config.Get<SetupStepFactorySettings>(typeof(DeploymentJsonConfiguration));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Need static constructor for Its.Configuration initialization.")]
        static AdjustDeploymentTest()
        {
            MessageBusHarnessPackageBytes =
                File.ReadAllBytes(
                    Path.Combine(
                        Path.Combine(
                            new Uri(typeof(AdjustDeploymentTest).Assembly.CodeBase).LocalPath.Replace("\\Naos.Deployment.Core.Test.DLL", string.Empty),
                            "..",
                            "..",
                            "TestingPackageFiles"),
                        "Naos.MessageBus.Hangfire.Console.1.0.233.testnupkg"));

            MessageBusHandlerHarnessConfiguration = new MessageBusHandlerHarnessConfiguration
                                                        {
                                                            LogWritingSettings = new LogWritingSettings(new[] { new InMemoryLogConfig(new Dictionary<LogItemKind, IReadOnlyCollection<string>>()), }),
                                                            HandlerHarnessProcessTimeToLive = TimeSpan.FromMinutes(1),
                                                            PersistenceConnectionConfiguration = new MessageBusConnectionConfiguration(),
                                                            Package = new PackageDescriptionWithOverrides
                                                                          {
                                                                              Id = "Naos.MessageBus.Hangfire.Console",
                                                                              InitializationStrategies =
                                                                                  new[]
                                                                                      {
                                                                                          new
                                                                                              InitializationStrategyScheduledTask
                                                                                                  {
                                                                                                      RunElevated = false,
                                                                                                      ScheduledTaskAccount = "Network Service",
                                                                                                      Arguments = "monitor",
                                                                                                      Description = "Hangfire Harness to reflect over and run on assemblies.",
                                                                                                      ExeFilePathRelativeToPackageRoot = "packagedConsoleApp/Naos.MessageBus.Hangfire.Console.exe",
                                                                                                      Name = "Hangfire Harness",
                                                                                                      Schedule = new IntervalSchedule { Interval = TimeSpan.FromMinutes(1) },
                                                                                                  },
                                                                                      },
                                                                          },
                                                        };

            DatabaseManagementConfiguration = new DatabaseManagementConfiguration
                                                   {
                                                       FileSystemManagementLogWritingSettings = new LogWritingSettings(new[] { new InMemoryLogConfig(new Dictionary<LogItemKind, IReadOnlyCollection<string>>()), }),
                                                       DatabaseManagementLogWritingSettings = new LogWritingSettings(new[] { new InMemoryLogConfig(new Dictionary<LogItemKind, IReadOnlyCollection<string>>()), }),
                                                       HandlerHarnessProcessTimeToLive = TimeSpan.FromMinutes(1),
                                                       PersistenceConnectionConfiguration = new MessageBusConnectionConfiguration(),
                                                       FileSystemManagementPackage = new PackageDescriptionWithOverrides
                                                                                         {
                                                                                             Id = "Naos.FileJanitor.MessageBus.Hangfire.Console",
                                                                                             InitializationStrategies =
                                                                                                 new[]
                                                                                                     {
                                                                                                         new
                                                                                                             InitializationStrategyScheduledTask
                                                                                                                 {
                                                                                                                     RunElevated = true,
                                                                                                                     ScheduledTaskAccount = "Network Service",
                                                                                                                     Arguments = "monitor",
                                                                                                                     Description = "FileJanitor Hangfire Harness to perform requested File System operations.",
                                                                                                                     ExeFilePathRelativeToPackageRoot = "packagedConsoleApp/Naos.FileJanitor.Hangfire.Console.exe",
                                                                                                                     Name = "FileJanitor Hangfire Harness",
                                                                                                                     Schedule = new IntervalSchedule { Interval = TimeSpan.FromMinutes(1) },
                                                                                                                 },
                                                                                                     },
                                                                                         },
                                                       DatabaseManagementPackage = new PackageDescriptionWithOverrides
                                                                                        {
                                                                                            Id = "Naos.Database.MessageBus.Hangfire.Console",
                                                                                            InitializationStrategies =
                                                                                                new[]
                                                                                                    {
                                                                                                        new
                                                                                                            InitializationStrategyScheduledTask
                                                                                                                {
                                                                                                                    RunElevated = true,
                                                                                                                    ScheduledTaskAccount = "Network Service",
                                                                                                                    Arguments = "monitor",
                                                                                                                    Description = "Database Hangfire Harness to perform requested database operations.",
                                                                                                                    ExeFilePathRelativeToPackageRoot = "packagedConsoleApp/Naos.Database.Hangfire.Console.exe",
                                                                                                                    Name = "Database Hangfire Harness",
                                                                                                                    Schedule = new IntervalSchedule { Interval = TimeSpan.FromMinutes(1) },
                                                                                                                },
                                                                                                    },
                                                                                        },
                                                   };

            ConfigFileManager = new ConfigFileManager(new[] { Config.CommonPrecedence }, Config.DefaultConfigDirectoryName, new NaosJsonSerializer(typeof(DeploymentJsonConfiguration)));

            DeploymentConfig = new DeploymentConfiguration
                                   {
                                       ChocolateyPackages = new[] { new PackageDescription { Id = "NotepadPlusPlus" } },
                                       DeploymentStrategy = new DeploymentStrategy { RunSetupSteps = true, IncludeInstanceInitializationScript = true },
                                       InstanceAccessibility = InstanceAccessibility.Private,
                                       InstanceCount = 1,
                                       InstanceType = new InstanceType { VirtualCores = 4, RamInGb = 48, OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.Base } },
                                       PostDeploymentStrategy = new PostDeploymentStrategy { TurnOffInstance = false },
                                       Volumes = new[] { new Volume { DriveLetter = "C", SizeInGb = 100, Type = VolumeType.HighPerformance } },
                                   };
        }

        [Fact]
        public static void MessageBusHarnessAdder___IsMatch__with_one_MessageBus_initialization_without_harness_package___Is_true()
        {
            // Arrange
            var adder = new MessageBusHarnessAdder(MessageBusHandlerHarnessConfiguration);
            var packagesToDeploy = BuildPackagesToDeploy(new[] { new InitializationStrategyMessageBusHandler { ChannelsToMonitor = new[] { new SimpleChannel("work") }, WorkerCount = 1, }, }, "Pretend");

            // Act
            var wasMatch = adder.IsMatch(ConfigFileManager, packagesToDeploy, DeploymentConfig);

            // Assert
            wasMatch.Should().BeTrue();
        }

        [Fact]
        public static void MessageBusHarnessAdder___IsMatch__with_one_MessageBus_initialization_with_harness_package___Is_false()
        {
            // Arrange
            var adder = new MessageBusHarnessAdder(MessageBusHandlerHarnessConfiguration);
            var packagesToDeploy = BuildPackagesToDeploy(
                new[] { new InitializationStrategyMessageBusHandler { ChannelsToMonitor = new[] { new SimpleChannel("work") }, WorkerCount = 1, }, }, MessageBusHandlerHarnessConfiguration.Package.Id);

            // Act
            var wasMatch = adder.IsMatch(ConfigFileManager, packagesToDeploy, DeploymentConfig);

            // Assert
            wasMatch.Should().BeFalse();
        }

        [Fact]
        public static void MessageBusHarnessAdder___IsMatch_and_GetAdditionalPackages_with_zero_MessageBus_initialization_without_other_message_bus_packages___Is_false_and_empty_set()
        {
            // Arrange
            var adder = new MessageBusHarnessAdder(MessageBusHandlerHarnessConfiguration);
            var packagesToDeploy = BuildPackagesToDeploy(new[] { new InitializationStrategyCertificateToInstall { CertificateToInstall = "MyCert" } }, "Pretend");

            // Act
            var wasMatch = adder.IsMatch(ConfigFileManager, packagesToDeploy, DeploymentConfig);

            IReadOnlyCollection<InjectedPackage> packages = null;
            using (var packageManager = new PackageRetriever(
                Path.GetTempPath(),
                new[] { PackageRepositoryConfiguration.NugetOrgV3 },
                null,
                s =>
                    {
                        /*no-op*/
                    }))
            {
                packages = adder.GetAdditionalPackages(
                    "ExampleDevelopment",
                    "MyPretendServer",
                    0,
                    ConfigFileManager,
                    packagesToDeploy,
                    DeploymentConfig,
                    new PackageHelper(packageManager, new[] { "netStandard" }, Path.GetTempPath()),
                    StepFactorySettings);
            }

            // Assert
            wasMatch.Should().BeFalse();
            packages.Should().NotBeNull();
            packages.Count.Should().Be(0);
        }

        [Fact]
        public static void MessageBusHarnessAdder___GetAdditionalPackages__with_one_MessageBus_initialization_without_other_message_bus_packages___Gets_expected_and_configured_package()
        {
            // Arrange
            var adder = new MessageBusHarnessAdder(MessageBusHandlerHarnessConfiguration);
            var packagesToDeploy = BuildPackagesToDeploy(new[] { new InitializationStrategyMessageBusHandler { ChannelsToMonitor = new[] { new SimpleChannel("work") }, WorkerCount = 1, }, }, "Pretend");

            // Act
            IReadOnlyCollection<InjectedPackage> packages = null;
            using (var packageManager = new PackageRetriever(
                Path.GetTempPath(),
                new[] { PackageRepositoryConfiguration.NugetOrgV3 },
                null,
                s =>
                    {
                        /*no-op*/
                    }))
            {
                packages = adder.GetAdditionalPackages(
                    "ExampleDevelopment",
                    "MyPretendServer",
                    0,
                    ConfigFileManager,
                    packagesToDeploy,
                    DeploymentConfig,
                    new PackageHelper(packageManager, new[] { "netStandard" }, Path.GetTempPath()),
                    StepFactorySettings);
            }

            // Assert
            packages.Should().NotBeNull();
            packages.Count.Should().Be(1);
        }

        [Fact]
        public static void SqlServerManagementAdder___IsMatch_and_GetAdditionalPackages_with_zero_SqlServer_initialization___Is_false_and_empty_set()
        {
            // Arrange
            var adder = new DatabaseManagementAdder(DatabaseManagementConfiguration);
            var packagesToDeploy = BuildPackagesToDeploy(new[] { new InitializationStrategyCertificateToInstall { CertificateToInstall = "MyCert" } }, "Pretend");

            // Act
            var wasMatch = adder.IsMatch(ConfigFileManager, packagesToDeploy, DeploymentConfig);

            IReadOnlyCollection<InjectedPackage> packages = null;
            using (var packageManager = new PackageRetriever(
                Path.GetTempPath(),
                new[] { PackageRepositoryConfiguration.NugetOrgV3 },
                null,
                s =>
                    {
                        /*no-op*/
                    }))
            {
                packages = adder.GetAdditionalPackages(
                    "ExampleDevelopment",
                    "MyPretendServer",
                    0,
                    ConfigFileManager,
                    packagesToDeploy,
                    DeploymentConfig,
                    new PackageHelper(packageManager, new[] { "netStandard" }, Path.GetTempPath()),
                    StepFactorySettings);
            }

            // Assert
            wasMatch.Should().BeFalse();
            packages.Should().NotBeNull();
            packages.Count.Should().Be(0);
        }

        [Fact]
        public static void SqlServerManagementAdder___IsMatch_and_GetAdditionalPackages_with_SqlServer_initialization___Is_true_and_has_expected_packages()
        {
            // Arrange
            var adder = new DatabaseManagementAdder(DatabaseManagementConfiguration);
            var packagesToDeploy = BuildPackagesToDeploy(new[] { new InitializationStrategySqlServer { AdministratorPassword = "password", ManagementChannelName = "pretend", Name = "MyDatabase" } }, "Pretend");

            // Act
            var wasMatch = adder.IsMatch(ConfigFileManager, packagesToDeploy, DeploymentConfig);

            IReadOnlyCollection<InjectedPackage> packages = null;
            using (var packageManager = new PackageRetriever(
                Path.GetTempPath(),
                new[] { PackageRepositoryConfiguration.NugetOrgV3 },
                null,
                s =>
                    {
                        /*no-op*/
                    }))
            {
                var setupStepFactorySettings = StepFactorySettings;
                setupStepFactorySettings.DatabaseServerSettings.Should().NotBeNull();

                packages = adder.GetAdditionalPackages(
                    "ExampleDevelopment",
                    "MyPretendServer",
                    0,
                    ConfigFileManager,
                    packagesToDeploy,
                    DeploymentConfig,
                    new PackageHelper(packageManager, new[] { "netStandard" }, Path.GetTempPath()),
                    setupStepFactorySettings);
            }

            // Assert
            wasMatch.Should().BeTrue();
            packages.Should().NotBeNull();
            packages.Count.Should().Be(2);
        }

        private static PackagedDeploymentConfiguration[] BuildPackagesToDeploy(IReadOnlyCollection<InitializationStrategyBase> initializationStrategies, string packageId)
        {
            return new[]
                       {
                           new PackagedDeploymentConfiguration
                               {
                                   DeploymentConfiguration = DeploymentConfig,
                                   InitializationStrategies = initializationStrategies,
                                   PackageWithBundleIdentifier = new PackageWithBundleIdentifier
                                                                     {
                                                                         AreDependenciesBundled = false,
                                                                         Package = new Package
                                                                                       {
                                                                                           PackageDescription = new PackageDescription
                                                                                                                    {
                                                                                                                        Id = packageId,
                                                                                                                    },
                                                                                           PackageFileBytes = MessageBusHarnessPackageBytes, // just to have some valid package bytes...
                                                                                           PackageFileBytesRetrievalDateTimeUtc = DateTime.UtcNow,
                                                                                       },
                                                                     },
                               },
                       };
        }
    }
}
