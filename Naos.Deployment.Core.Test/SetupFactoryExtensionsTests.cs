// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupFactoryExtensionsTests.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    using FluentAssertions;

    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;
    using Naos.Logging.Persistence;
    using Naos.Serialization.Json;

    using Xunit;
    using Xunit.Sdk;

    public static class SetupFactoryExtensionsTests
    {
        [Fact]
        public static void ExecutionOrderWorks()
        {
            // Arrange
            var expected = new[]
                               {
                                   ExecutionOrder.Invalid, ExecutionOrder.InstanceLevel, ExecutionOrder.CopyPackages, ExecutionOrder.OneTimeBeforeReboot,
                                   ExecutionOrder.OneTimeAfterRebootFirst, ExecutionOrder.OneTimeAfterRebootLast, ExecutionOrder.Dns,
                               };

            var input = expected.OrderBy(_ => _.ToString()).ToList();

            // Act
            var actual = input.OrderBy(_ => _).ToList();
            var actualCasted = input.OrderBy(_ => (int)_).ToList();

            // Assert
            expected.Should().NotEqual(input);
            actual.Should().Equal(expected);
            actualCasted.Should().Equal(expected);
        }

        [Fact]
        public static void BuildRootDeploymentPath___Only_c_volume___Chooses_c()
        {
            // Arrange
            var settings = new SetupStepFactorySettings
                               {
                                   RootDeploymentPathTemplate = "{deploymentDriveLetter}:\\Deployments",
                                   DeploymentDriveLetterPrecedence = new[] { "D", "C" },
                               };

            var volumes = new[] { new Volume { DriveLetter = "C" } };
            var expected = "C:\\Deployments";

            // Act
            var actual = settings.BuildRootDeploymentPath(volumes);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void BuildRootDeploymentPath___C_and_d_volumes___Chooses_d()
        {
            // Arrange
            var settings = new SetupStepFactorySettings
                               {
                                   RootDeploymentPathTemplate = "{deploymentDriveLetter}:\\Deployments",
                                   DeploymentDriveLetterPrecedence = new[] { "D", "C" },
                               };

            var volumes = new[] { new Volume { DriveLetter = "C" }, new Volume { DriveLetter = "D" } };
            var expected = "D:\\Deployments";

            // Act
            var actual = settings.BuildRootDeploymentPath(volumes);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void BuildRootDeploymentPath___No_c_and_d_volumes___Throws()
        {
            // Arrange
            var settings = new SetupStepFactorySettings
                               {
                                   RootDeploymentPathTemplate = "{deploymentDriveLetter}:\\Deployments",
                                   DeploymentDriveLetterPrecedence = new[] { "D", "C" },
                               };

            var volumes = new[] { new Volume { DriveLetter = "F" } };

            // Act
            var exception = Record.Exception(() => settings.BuildRootDeploymentPath(volumes));

            // Assert
            exception.Should().NotBeNull();
            exception.Message.Should().Be("Must specify a drive in the DeploymentDriveLetterPrecedence; expected one of (D,C); found (F).");
        }

        [Fact]
        public static void BuildDefaultLogWritingSettings___Not_file_based___Throws()
        {
            // Arrange
            var settingsConsole = new SetupStepFactorySettings
                               {
                                   DefaultLogWritingSettings = new LogWritingSettings(
                                       new[] { new ConsoleLogConfig(new Dictionary<LogItemKind, IReadOnlyCollection<string>>(), new Dictionary<LogItemKind, IReadOnlyCollection<string>>(), new Dictionary<LogItemKind, IReadOnlyCollection<string>>()), }),
                               };
            var settingsEvent = new SetupStepFactorySettings
                               {
                                   DefaultLogWritingSettings = new LogWritingSettings(
                                       new[] { new EventLogConfig(new Dictionary<LogItemKind, IReadOnlyCollection<string>>()), }),
                               };
            var settingsMemory = new SetupStepFactorySettings
                               {
                                   DefaultLogWritingSettings = new LogWritingSettings(
                                       new[] { new InMemoryLogConfig(new Dictionary<LogItemKind, IReadOnlyCollection<string>>()), }),
                               };

            // Act
            var exceptionConsole = Record.Exception(() => settingsConsole.BuildDefaultLogWritingSettings("path", "package"));
            var exceptionEvent = Record.Exception(() => settingsEvent.BuildDefaultLogWritingSettings("path", "package"));
            var exceptionMemory = Record.Exception(() => settingsMemory.BuildDefaultLogWritingSettings("path", "package"));

            // Assert
            exceptionConsole.Should().BeOfType<NotSupportedException>().Which.Message.Should().Be("Unsupported LogWriterConfigBase in SetupStepFactorySettings.DefaultLogWritingSettings; Naos.Logging.Domain.ConsoleLogConfig");
            exceptionEvent.Should().BeOfType<NotSupportedException>().Which.Message.Should().Be("Unsupported LogWriterConfigBase in SetupStepFactorySettings.DefaultLogWritingSettings; Naos.Logging.Domain.EventLogConfig");
            exceptionMemory.Should().BeOfType<NotSupportedException>().Which.Message.Should().Be("Unsupported LogWriterConfigBase in SetupStepFactorySettings.DefaultLogWritingSettings; Naos.Logging.Domain.InMemoryLogConfig");
        }

        [Fact]
        public static void BuildDefaultLogWritingSettings___File_with_file_name___Prefixes_with_path_and_provided_name()
        {
            // Arrange
            const bool CreateDirectoryStructureIfMissing = false;
            var settings = new SetupStepFactorySettings
                               {
                                   DefaultLogWritingSettings = new LogWritingSettings(
                                       new[] { new FileLogConfig(new Dictionary<LogItemKind, IReadOnlyCollection<string>>(), "{deploymentDriveLetter}:\\Logs\\MyLog.txt", CreateDirectoryStructureIfMissing), }),
                               };

            var deploymentDriveLetter = "C";
            var packageName = "Naos.Something.Awesome";
            var expected = "C:\\Logs\\Naos.Something.Awesome-MyLog.txt";

            // Act
            var actual = settings.BuildDefaultLogWritingSettings(deploymentDriveLetter, packageName);

            // Assert
            actual.Configs.Single().As<FileLogConfig>().LogInclusionKindToOriginsMap.Count.Should().Be(0);
            actual.Configs.Single().As<FileLogConfig>().LogFilePath.Should().Be(expected);
            actual.Configs.Single().As<FileLogConfig>().CreateDirectoryStructureIfMissing.Should().Be(CreateDirectoryStructureIfMissing);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "subpath", Justification = "Spelling/name is correct.")]
        [Fact]
        public static void BuildDefaultLogWritingSettings___File_with_sub_path_with_slash_and_file_name___Prefixes_with_path_and_then_file_with_name()
        {
            // Arrange
            const bool CreateDirectoryStructureIfMissing = false;
            var settings = new SetupStepFactorySettings
                               {
                                   DefaultLogWritingSettings = new LogWritingSettings(
                                       new[] { new FileLogConfig(new Dictionary<LogItemKind, IReadOnlyCollection<string>>(), "{deploymentDriveLetter}:\\Logs\\Path\\MyLog.txt", CreateDirectoryStructureIfMissing), }),
                               };

            var deploymentDriveLetter = "C";
            var packageName = "Naos.Something.Awesome";
            var expected = "C:\\Logs\\Path\\Naos.Something.Awesome-MyLog.txt";

            // Act
            var actual = settings.BuildDefaultLogWritingSettings(deploymentDriveLetter, packageName);

            // Assert
            actual.Configs.Single().As<FileLogConfig>().LogInclusionKindToOriginsMap.Count.Should().Be(0);
            actual.Configs.Single().As<FileLogConfig>().LogFilePath.Should().Be(expected);
            actual.Configs.Single().As<FileLogConfig>().CreateDirectoryStructureIfMissing.Should().Be(CreateDirectoryStructureIfMissing);
        }

        [Fact]
        public static void BuildDefaultLogWritingSettings___Sliced_with_file_name___Prefixes_with_path_and_provided_name()
        {
            // Arrange
            const bool CreateDirectoryStructureIfMissing = false;
            var time = TimeSpan.FromMinutes(10);
            var settings = new SetupStepFactorySettings
                               {
                                   DefaultLogWritingSettings = new LogWritingSettings(
                                       new[] { new TimeSlicedFilesLogConfig(new Dictionary<LogItemKind, IReadOnlyCollection<string>>(), "{deploymentDriveLetter}:\\Logs", "Prefix", time, CreateDirectoryStructureIfMissing), }),
                               };

            var deploymentDriveLetter = "C";
            var packageName = "Naos.Something.Awesome";
            var expectedPath = "C:\\Logs\\Naos.Something.Awesome";
            var expectedPrefix = "Naos.Something.Awesome-Prefix";

            // Act
            var actual = settings.BuildDefaultLogWritingSettings(deploymentDriveLetter, packageName);

            // Assert
            actual.Configs.Single().As<TimeSlicedFilesLogConfig>().LogInclusionKindToOriginsMap.Count().Should().Be(0);
            actual.Configs.Single().As<TimeSlicedFilesLogConfig>().LogFileDirectoryPath.Should().Be(expectedPath);
            actual.Configs.Single().As<TimeSlicedFilesLogConfig>().FileNamePrefix.Should().Be(expectedPrefix);
            actual.Configs.Single().As<TimeSlicedFilesLogConfig>().TimeSlicePerFile.Should().Be(time);
            actual.Configs.Single().As<TimeSlicedFilesLogConfig>().CreateDirectoryStructureIfMissing.Should().Be(CreateDirectoryStructureIfMissing);
        }
    }
}
