// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupFactoryExtensionsTests.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.IO;
    using System.Linq;

    using FluentAssertions;

    using Its.Configuration;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;
    using Naos.Packaging.NuGet;
    using Naos.Recipes.Configuration.Setup;

    using Newtonsoft.Json;

    using Spritely.Recipes;

    using Xunit;

    using static System.FormattableString;

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
        public static void BuildDefaultLoggingPath___Only_c_volume___Chooses_c()
        {
            // Arrange
            var settings = new SetupStepFactorySettings
                               {
                                   DefaultLoggingPathTemplate = "{deploymentDriveLetter}:\\Logs",
                                   DeploymentDriveLetterPrecedence = new[] { "D", "C" },
                               };

            var volumes = new[] { new Volume { DriveLetter = "C" } };
            var expected = "C:\\Logs";

            // Act
            var actual = settings.BuildDefaultLoggingPath(volumes);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void BuildDefaultLoggingPath___C_and_d_volumes___Chooses_d()
        {
            // Arrange
            var settings = new SetupStepFactorySettings
                               {
                                   DefaultLoggingPathTemplate = "{deploymentDriveLetter}:\\Logs",
                                   DeploymentDriveLetterPrecedence = new[] { "D", "C" },
                               };

            var volumes = new[] { new Volume { DriveLetter = "C" }, new Volume { DriveLetter = "D" } };
            var expected = "D:\\Logs";

            // Act
            var actual = settings.BuildDefaultLoggingPath(volumes);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void BuildDefaultLoggingPath___No_c_and_d_volumes___Throws()
        {
            // Arrange
            var settings = new SetupStepFactorySettings
                               {
                                   DefaultLoggingPathTemplate = "{deploymentDriveLetter}:\\Logs",
                                   DeploymentDriveLetterPrecedence = new[] { "D", "C" },
                               };

            var volumes = new[] { new Volume { DriveLetter = "F" } };

            // Act
            var exception = Record.Exception(() => settings.BuildDefaultLoggingPath(volumes));

            // Assert
            exception.Should().NotBeNull();
            exception.Message.Should().Be("Must specify a drive in the DeploymentDriveLetterPrecedence; expected one of (D,C); found (F).");
        }
    }
}
