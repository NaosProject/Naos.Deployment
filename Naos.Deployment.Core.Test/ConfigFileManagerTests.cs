// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigFileManagerTests.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System.Linq;

    using FluentAssertions;

    using Naos.Deployment.Domain;
    using Naos.Serialization.Factory;

    using Xunit;

    using static System.FormattableString;

    public static class ConfigFileManagerTests
    {
        [Fact]
        public static void BuildPrecedenceChain___With_no_precedence___Gets_common()
        {
            // Arrange
            var expected = new[] { Config.CommonPrecedence };
            var manager = new ConfigFileManager(
                expected,
                Config.DefaultConfigDirectoryName,
                SerializerFactory.Instance.BuildSerializer(Config.ConfigFileSerializationDescription));

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
                SerializerFactory.Instance.BuildSerializer(Config.ConfigFileSerializationDescription));
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
                SerializerFactory.Instance.BuildSerializer(Config.ConfigFileSerializationDescription));

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
                SerializerFactory.Instance.BuildSerializer(Config.ConfigFileSerializationDescription));

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
                SerializerFactory.Instance.BuildSerializer(Config.ConfigFileSerializationDescription));

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
                SerializerFactory.Instance.BuildSerializer(Config.ConfigFileSerializationDescription));

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
                SerializerFactory.Instance.BuildSerializer(Config.ConfigFileSerializationDescription));

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
                SerializerFactory.Instance.BuildSerializer(Config.ConfigFileSerializationDescription));

            var precedence = "precedence";
            var expected = Invariant($"{Config.DefaultConfigDirectoryName}\\{precedence}");

            // Act
            var actual = manager.BuildConfigPath(precedence: precedence);

            // Assert
            actual.Should().Be(expected);
        }
    }
}
