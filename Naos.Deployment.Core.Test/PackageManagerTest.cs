// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageManagerTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Xml.Serialization;

    using Naos.Deployment.Contract;

    using Xunit;

    public class PackageManagerTest
    {
        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameCollection_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameDuplicatesInFirst_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = "1.0.0" }, new PackageDescription() { Id = "monkey", Version = "1.1.0" } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameDuplicatesInSecond_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = "1.1.0" }, new PackageDescription() { Id = "monkey", Version = "1.0.0" } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameDuplicatesInBoth_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = "1.0.0" }, new PackageDescription() { Id = "monkey", Version = "1.1.0" } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = "1.1.0" }, new PackageDescription() { Id = "monkey", Version = "1.0.0" } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_Different_ReturnsFalse()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = null }, new PackageDescription() { Id = "ape", Version = null } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = false;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void BuildConfigFileFromRepositoryConfigurationThenSerializeNuGetConfig_ValidObject_ValidXml()
        {
            var repoConfig = new PackageRepositoryConfiguration
                                 {
                                     Source = "http://theurl",
                                     SourceName = "MyCustomSource",
                                     Username = "A.User",
                                     ClearTextPassword = "DontForgetMe"
                                 };

            var config = NuGetConfigFile.BuildConfigFileFromRepositoryConfiguration(repoConfig);

            var actualXml = NuGetConfigFile.Serialize(config);
            var expectedXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine +
"<configuration>" + Environment.NewLine +
"  <activePackageSource>" + Environment.NewLine +
"    <add key=\"nuget.org\" value=\"https://www.nuget.org/api/v2/\" />" + Environment.NewLine +
"  </activePackageSource>" + Environment.NewLine +
"  <packageSources>" + Environment.NewLine +
"    <add key=\"nuget.org\" value=\"https://www.nuget.org/api/v2/\" />" + Environment.NewLine +
"    <add key=\"" + repoConfig.SourceName + "\" value=\"" + repoConfig.Source + "\" />" + Environment.NewLine +
"  </packageSources>" + Environment.NewLine +
"  <packageSourceCredentials>" + Environment.NewLine +
"    <" + repoConfig.SourceName + ">" + Environment.NewLine +
"      <add key=\"Username\" value=\"" + repoConfig.Username + "\" />" + Environment.NewLine +
"      <add key=\"Password\" value=\"" + repoConfig.ClearTextPassword + "\" />" + Environment.NewLine +
"    </" + repoConfig.SourceName + ">" + Environment.NewLine +
"  </packageSourceCredentials>" + Environment.NewLine +
"</configuration>";
            Assert.Equal(expectedXml, actualXml);
        }
    }
}
