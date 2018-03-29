// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupFactoryExtensionsTests.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Linq;

    using FluentAssertions;

    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;
    using Naos.Serialization.Json;

    using Xunit;
    using Xunit.Sdk;

    public static class SetupFactoryExtensionsTests
    {
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
        public static void BuildDefaultLogProcessorSettings___Not_file_based___Throws()
        {
            // Arrange
            var settingsConsole = new SetupStepFactorySettings
                               {
                                   DefaultLogProcessorSettings = new LogProcessorSettings(
                                       new[] { new ConsoleLogConfiguration(LogContexts.All, LogContexts.AllErrors), }),
                               };
            var settingsEvent = new SetupStepFactorySettings
                               {
                                   DefaultLogProcessorSettings = new LogProcessorSettings(
                                       new[] { new EventLogConfiguration(LogContexts.All), }),
                               };
            var settingsMemory = new SetupStepFactorySettings
                               {
                                   DefaultLogProcessorSettings = new LogProcessorSettings(
                                       new[] { new InMemoryLogConfiguration(LogContexts.All), }),
                               };

            // Act
            var exceptionConsole = Record.Exception(() => settingsConsole.BuildDefaultLogProcessorSettings("path", "package"));
            var exceptionEvent = Record.Exception(() => settingsEvent.BuildDefaultLogProcessorSettings("path", "package"));
            var exceptionMemory = Record.Exception(() => settingsMemory.BuildDefaultLogProcessorSettings("path", "package"));

            // Assert
            exceptionConsole.Should().BeOfType<NotSupportedException>().Which.Message.Should().Be("Unsupported LogConfigurationBase in SetupStepFactorySettings.DefaultLogProcessorSettings; Naos.Logging.Domain.ConsoleLogConfiguration");
            exceptionEvent.Should().BeOfType<NotSupportedException>().Which.Message.Should().Be("Unsupported LogConfigurationBase in SetupStepFactorySettings.DefaultLogProcessorSettings; Naos.Logging.Domain.EventLogConfiguration");
            exceptionMemory.Should().BeOfType<NotSupportedException>().Which.Message.Should().Be("Unsupported LogConfigurationBase in SetupStepFactorySettings.DefaultLogProcessorSettings; Naos.Logging.Domain.InMemoryLogConfiguration");
        }

        [Fact]
        public static void BuildDefaultLogProcessorSettings___File_with_file_name___Prefixes_with_path_and_provided_name()
        {
            // Arrange
            const bool CreateDirectoryStructureIfMissing = false;
            var settings = new SetupStepFactorySettings
                               {
                                   DefaultLogProcessorSettings = new LogProcessorSettings(
                                       new[] { new FileLogConfiguration(LogContexts.All, "{deploymentDriveLetter}:\\Logs\\MyLog.txt", CreateDirectoryStructureIfMissing), }),
                               };

            var deploymentDriveLetter = "C";
            var packageName = "Naos.Something.Awesome";
            var expected = "C:\\Logs\\Naos.Something.Awesome-MyLog.txt";

            // Act
            var actual = settings.BuildDefaultLogProcessorSettings(deploymentDriveLetter, packageName);

            // Assert
            actual.Configurations.Single().As<FileLogConfiguration>().ContextsToLog.Should().Be(settings.DefaultLogProcessorSettings.Configurations.Single().ContextsToLog);
            actual.Configurations.Single().As<FileLogConfiguration>().LogFilePath.Should().Be(expected);
            actual.Configurations.Single().As<FileLogConfiguration>().CreateDirectoryStructureIfMissing.Should().Be(CreateDirectoryStructureIfMissing);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "subpath", Justification = "Spelling/name is correct.")]
        [Fact]
        public static void BuildDefaultLogProcessorSettings___File_with_sub_path_with_slash_and_file_name___Prefixes_with_path_and_then_file_with_name()
        {
            // Arrange
            const bool CreateDirectoryStructureIfMissing = false;
            var settings = new SetupStepFactorySettings
                               {
                                   DefaultLogProcessorSettings = new LogProcessorSettings(
                                       new[] { new FileLogConfiguration(LogContexts.All, "{deploymentDriveLetter}:\\Logs\\Path\\MyLog.txt", CreateDirectoryStructureIfMissing), }),
                               };

            var deploymentDriveLetter = "C";
            var packageName = "Naos.Something.Awesome";
            var expected = "C:\\Logs\\Path\\Naos.Something.Awesome-MyLog.txt";

            // Act
            var actual = settings.BuildDefaultLogProcessorSettings(deploymentDriveLetter, packageName);

            // Assert
            actual.Configurations.Single().As<FileLogConfiguration>().ContextsToLog.Should().Be(settings.DefaultLogProcessorSettings.Configurations.Single().ContextsToLog);
            actual.Configurations.Single().As<FileLogConfiguration>().LogFilePath.Should().Be(expected);
            actual.Configurations.Single().As<FileLogConfiguration>().CreateDirectoryStructureIfMissing.Should().Be(CreateDirectoryStructureIfMissing);
        }

        [Fact]
        public static void BuildDefaultLogProcessorSettings___Sliced_with_file_name___Prefixes_with_path_and_provided_name()
        {
            // Arrange
            const bool CreateDirectoryStructureIfMissing = false;
            var time = TimeSpan.FromMinutes(10);
            var settings = new SetupStepFactorySettings
                               {
                                   DefaultLogProcessorSettings = new LogProcessorSettings(
                                       new[] { new TimeSlicedFilesLogConfiguration(LogContexts.All, "{deploymentDriveLetter}:\\Logs", "Prefix", time, CreateDirectoryStructureIfMissing), }),
                               };

            var deploymentDriveLetter = "C";
            var packageName = "Naos.Something.Awesome";
            var expectedPath = "C:\\Logs\\Naos.Something.Awesome";
            var expectedPrefix = "Naos.Something.Awesome-Prefix";

            // Act
            var actual = settings.BuildDefaultLogProcessorSettings(deploymentDriveLetter, packageName);

            // Assert
            actual.Configurations.Single().As<TimeSlicedFilesLogConfiguration>().ContextsToLog.Should().Be(settings.DefaultLogProcessorSettings.Configurations.Single().ContextsToLog);
            actual.Configurations.Single().As<TimeSlicedFilesLogConfiguration>().LogFileDirectoryPath.Should().Be(expectedPath);
            actual.Configurations.Single().As<TimeSlicedFilesLogConfiguration>().FileNamePrefix.Should().Be(expectedPrefix);
            actual.Configurations.Single().As<TimeSlicedFilesLogConfiguration>().TimeSlicePerFile.Should().Be(time);
            actual.Configurations.Single().As<TimeSlicedFilesLogConfiguration>().CreateDirectoryStructureIfMissing.Should().Be(CreateDirectoryStructureIfMissing);
        }
    }
}
