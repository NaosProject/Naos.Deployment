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
        public static void GetVersionFromNuSpecFile_NullContents_ReturnsNull()
        {
            var packageManager = new PackageManager(null, null);
            var version = packageManager.GetVersionFromNuSpecFile(null);
            Assert.Null(version);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_EmptyContents_ReturnsNull()
        {
            var packageManager = new PackageManager(null, null);
            var version = packageManager.GetVersionFromNuSpecFile(string.Empty);
            Assert.Null(version);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_InvalidContents_Throws()
        {
            var packageManager = new PackageManager(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile("NOT XML..."));
            Assert.Equal("NuSpec contents is not valid to be parsed.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContentsMultipleMetadata_Throws()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <id>Naos.Something</id>
    <version>1.0.300</version>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
  <metadata>
    <id>Naos.Something</id>
    <version>1.0.300</version>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
</package>";

            var packageManager = new PackageManager(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile(nuSpecFileContents));
            Assert.Equal("Found multiple metadata nodes in the provided NuSpec.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContentsMissingMetadata_Throws()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
</package>";

            var packageManager = new PackageManager(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile(nuSpecFileContents));
            Assert.Equal("Could not find metadata in the provided NuSpec.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContentsMultipleVersions_Throws()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <id>Naos.Something</id>
    <version>1.0.299</version>
    <version>1.0.300</version>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
</package>";

            var packageManager = new PackageManager(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile(nuSpecFileContents));
            Assert.Equal("Found multiple version nodes in the provided NuSpec.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContentsMissingVersion_Throws()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <id>Naos.Something</id>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
</package>";

            var packageManager = new PackageManager(null, null);
            var ex = Assert.Throws<ArgumentException>(() => packageManager.GetVersionFromNuSpecFile(nuSpecFileContents));
            Assert.Equal("Could not find the version in the provided NuSpec.", ex.Message);
        }

        [Fact]
        public static void GetVersionFromNuSpecFile_ValidContents_ValidResult()
        {
            var nuSpecFileContents = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <id>Naos.Something</id>
    <version>1.0.299</version>
    <authors>APPVYR-WIN20122\appveyor</authors>
    <owners>APPVYR-WIN20122\appveyor</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Created on 2015-08-18 22:10</description>
    <copyright>Copyright 2015</copyright>
    <dependencies>
      <dependency id=""AWSSDK"" version=""2.3.50.1"" />
    </dependencies>
  </metadata>
</package>";

            var packageManager = new PackageManager(null, null);
            var version = packageManager.GetVersionFromNuSpecFile(nuSpecFileContents);
            Assert.Equal("1.0.299", version);
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
"    <add key=\"" + repoConfig.SourceName + "\" value=\"" + repoConfig.Source + "\" />" + Environment.NewLine +
"  </activePackageSource>" + Environment.NewLine +
"  <packageSources>" + Environment.NewLine +
"    <add key=\"nuget.org\" value=\"https://www.nuget.org/api/v2/\" />" + Environment.NewLine +
"    <add key=\"" + repoConfig.SourceName + "\" value=\"" + repoConfig.Source + "\" />" + Environment.NewLine +
"  </packageSources>" + Environment.NewLine +
"  <packageSourceCredentials>" + Environment.NewLine +
"    <" + repoConfig.SourceName + ">" + Environment.NewLine +
"      <add key=\"Username\" value=\"" + repoConfig.Username + "\" />" + Environment.NewLine +
"      <add key=\"ClearTextPassword\" value=\"" + repoConfig.ClearTextPassword + "\" />" + Environment.NewLine +
"      <add key=\"Password\" value=\"\" />" + Environment.NewLine +
"    </" + repoConfig.SourceName + ">" + Environment.NewLine +
"  </packageSourceCredentials>" + Environment.NewLine +
"</configuration>";
            Assert.Equal(expectedXml, actualXml);
        }
    }
}
