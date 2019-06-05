// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigFileManagerTests.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System.Linq;

    using FluentAssertions;
    using Naos.Configuration.Domain;
    using Naos.Deployment.Domain;
    using Naos.Serialization.Domain;
    using Naos.Serialization.Factory;
    using Naos.Serialization.Json;
    using Xunit;

    using static System.FormattableString;

    public static class ConfigFileManagerTests
    {
        private static readonly ISerializeAndDeserialize Serializer =
            new NaosJsonSerializer(typeof(NaosDeploymentCoreJsonConfiguration));

        [Fact]
        public static void BuildPrecedenceChain___With_no_precedence___Gets_common()
        {
            // Arrange
            var expected = new[] { Config.CommonPrecedence };
            var manager = new ConfigFileManager(
                expected,
                Config.DefaultConfigDirectoryName,
                Serializer);

            // Act
            var actual = manager.BuildPrecedenceChain();

            // Assert
            actual.Should().Equal(expected);
        }

        [Fact]
        public static void BuildPrecedenceChain___With_precedence___Gets_provided_followed_by_common()
        {
            // Arrange
            var common = new[] { Config.CommonPrecedence };
            var manager = new ConfigFileManager(
                common,
                Config.DefaultConfigDirectoryName,
                Serializer);
            var precedence = "precedence";
            var expected = new[] { precedence }.Concat(common).ToArray();

            // Act
            var actual = manager.BuildPrecedenceChain(precedence);

            // Assert
            actual.Should().Equal(expected);
        }

        [Fact]
        public static void BuildConfigPath___With_no_precedence___Uses_least_significant()
        {
            // Arrange
            var manager = new ConfigFileManager(
                new[] { "another", Config.CommonPrecedence },
                Config.DefaultConfigDirectoryName,
                Serializer);

            var file = "file";

            var expected = Invariant($"{Config.DefaultConfigDirectoryName}\\{Config.CommonPrecedence}\\{file}");

            // Act
            var actual = manager.BuildConfigPath(fileNameWithExtension: file);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void BuildConfigPath___With_root_and_precedence_and_file___Builds_full_path()
        {
            // Arrange
            var manager = new ConfigFileManager(
                new[] { Config.CommonPrecedence },
                Config.DefaultConfigDirectoryName,
                Serializer);

            var root = "root";
            var precedence = "precedence";
            var file = "file";
            var expected = Invariant($"{root}\\{Config.DefaultConfigDirectoryName}\\{precedence}\\{file}");

            // Act
            var actual = manager.BuildConfigPath(root, precedence, file);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void BuildConfigPath___With_precedence_and_file___Builds_partial_path()
        {
            // Arrange
            var manager = new ConfigFileManager(
                new[] { Config.CommonPrecedence },
                Config.DefaultConfigDirectoryName,
                Serializer);

            var precedence = "precedence";
            var file = "file";
            var expected = Invariant($"{Config.DefaultConfigDirectoryName}\\{precedence}\\{file}");

            // Act
            var actual = manager.BuildConfigPath(precedence: precedence, fileNameWithExtension: file);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void BuildConfigPath___With_root_and_precedence___Builds_partial_path()
        {
            // Arrange
            var manager = new ConfigFileManager(
                new[] { Config.CommonPrecedence },
                Config.DefaultConfigDirectoryName,
                Serializer);

            var root = "root";
            var precedence = "precedence";
            var expected = Invariant($"{root}\\{Config.DefaultConfigDirectoryName}\\{precedence}");

            // Act
            var actual = manager.BuildConfigPath(root, precedence);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void BuildConfigPath___With_root___Builds_partial_path()
        {
            // Arrange
            var manager = new ConfigFileManager(
                new[] { Config.CommonPrecedence },
                Config.DefaultConfigDirectoryName,
                Serializer);

            var root = "root";
            var expected = Invariant($"{root}\\{Config.DefaultConfigDirectoryName}\\{Config.CommonPrecedence}");

            // Act
            var actual = manager.BuildConfigPath(root);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void BuildConfigPath___With_precedence___Builds_partial_path()
        {
            // Arrange
            var manager = new ConfigFileManager(
                new[] { Config.CommonPrecedence },
                Config.DefaultConfigDirectoryName,
                Serializer);

            var precedence = "precedence";
            var expected = Invariant($"{Config.DefaultConfigDirectoryName}\\{precedence}");

            // Act
            var actual = manager.BuildConfigPath(precedence: precedence);

            // Assert
            actual.Should().Be(expected);
        }
    }
}
