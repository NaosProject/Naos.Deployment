// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Text;

    using Naos.Deployment.Contract;

    using NuGet;

    /// <summary>
    /// Implementation of the IManagePackages interface.
    /// </summary>
    public class PackageManager : IManagePackages
    {
        private readonly PackageRepositoryConfiguration repoConfig;
        private readonly string defaultWorkingDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageManager"/> class.
        /// </summary>
        /// <param name="repoConfig">Package repository configuration.</param>
        /// <param name="defaultWorkingDirectory">Working directory to download temporary files to.</param>
        public PackageManager(PackageRepositoryConfiguration repoConfig, string defaultWorkingDirectory)
        {
            this.repoConfig = repoConfig;
            this.defaultWorkingDirectory = defaultWorkingDirectory;
        }

        /// <summary>
        /// Designed to chain with the constructor this will make sure the default working directory is cleaned up (for failed previous runs).
        /// </summary>
        /// <returns>Current object to allow chaining with constructor.</returns>
        public PackageManager WithCleanWorkingDirectory()
        {
            if (Directory.Exists(this.defaultWorkingDirectory))
            {
                Directory.Delete(this.defaultWorkingDirectory, true);
            }

            Directory.CreateDirectory(this.defaultWorkingDirectory);
            return this;
        }

        /// <summary>
        /// Compares the distinct package IDs of two sets for equality.
        /// </summary>
        /// <param name="firstSet">First set of packages to compare.</param>
        /// <param name="secondSet">Second set of packages to compare.</param>
        /// <returns>Whether or not the distinct package IDs match exactly.</returns>
        public static bool DistinctPackageIdsMatchExactly(ICollection<PackageDescription> firstSet, ICollection<PackageDescription> secondSet)
        {
            if (firstSet.Count == 0 && secondSet.Count == 0)
            {
                return true;
            }

            var firstSetDistinctOrderedIds = firstSet.Select(_ => _.Id).ToList().Distinct().OrderBy(_ => _);
            var secondSetDistinctOrderedIds = secondSet.Select(_ => _.Id).ToList().Distinct().OrderBy(_ => _);

            var ret = firstSetDistinctOrderedIds.SequenceEqual(secondSetDistinctOrderedIds);
            return ret;
        }

        /// <inheritdoc />
        public string GetFileContentsFromPackageAsString(Package package, string searchPattern, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.ASCII;
            var retBytes = this.GetFileContentsFromPackageAsBytes(package, searchPattern);

            if (retBytes == null)
            {
                return null;
            }

            var retString = encoding.GetString(retBytes);
            return retString;
        }

        /// <inheritdoc />
        public byte[] GetFileContentsFromPackageAsBytes(Package package, string searchPattern)
        {
            // download package (decompressed)
            var workingDirectory = Path.Combine(this.defaultWorkingDirectory, "PackageFileContentsSearch-" + DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss"));
            var packageFilePath = Path.Combine(workingDirectory, "Package.zip");
            Directory.CreateDirectory(workingDirectory);
            File.WriteAllBytes(packageFilePath, package.PackageFileBytes);
            ZipFile.ExtractToDirectory(packageFilePath, Directory.GetParent(packageFilePath).FullName);

            // get list of files as fullpath strings
            var files = Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories);

            // normalize slashes in searchPattern AND in file list
            var normalizedSlashesSearchPattern = searchPattern.Replace(@"\", "/");
            var normalizedSlashesFiles = files.Select(_ => _.Replace(@"\", "/"));

            var fileToGetContentsFor =
                normalizedSlashesFiles.SingleOrDefault(
                    _ =>
                    CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                        _,
                        normalizedSlashesSearchPattern,
                        CompareOptions.IgnoreCase) >= 0);

            byte[] bytes = null;
            if (fileToGetContentsFor != null)
            {
                bytes = File.ReadAllBytes(fileToGetContentsFor);
            }
            
            // clean up temp files
            Directory.Delete(workingDirectory, true);

            return bytes;
        }

        /// <inheritdoc />
        public ICollection<string> DownloadPackages(ICollection<PackageDescription> packageDescriptions, string workingDirectory, bool includeDependencies = false)
        {
            // credential override for below taken from: http://stackoverflow.com/questions/18594613/setting-the-package-credentials-using-nuget-core-dll
            var settings = Settings.LoadDefaultSettings(null, null, null);
            var packageSource = new PackageSource(this.repoConfig.Source, this.repoConfig.SourceName)
            {
                UserName =
                    this.repoConfig
                    .Username,
                Password =
                    this.repoConfig
                    .Password
            };
            var packageSourceProvider = new PackageSourceProvider(settings, new[] { packageSource });
            var customCredentialProvider = new CustomCredentialProvider(this.repoConfig); // NullCredentialProvider.Instance;
            var credentialProvider = new SettingsCredentialProvider(customCredentialProvider, packageSourceProvider);
            HttpClient.DefaultCredentialProvider = credentialProvider;

            // logic taken from: http://blog.nuget.org/20130520/Play-with-packages.html
            var repo = packageSourceProvider.CreateAggregateRepository(PackageRepositoryFactory.Default, true);
            var packageManager = new NuGet.PackageManager(repo, workingDirectory);

            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }

            var workingDirectorySnapshotBefore = Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories);

            foreach (var packageDescription in packageDescriptions)
            {
                var version = SemanticVersion.ParseOptionalVersion(packageDescription.Version);
                var ignoreDependencies = !includeDependencies;
                packageManager.InstallPackage(packageDescription.Id, version, ignoreDependencies, true);
            }

            var workingDirectorySnapshotAfter = Directory.GetFiles(workingDirectory, "*", SearchOption.AllDirectories);

            var ret =
                workingDirectorySnapshotAfter.Except(workingDirectorySnapshotBefore)
                    .Where(_ => _.EndsWith(".nupkg"))
                    .ToList();

            return ret;
        }

        /// <inheritdoc />
        public byte[] GetPackageFile(PackageDescription packageDescription, bool bundleAllDependencies = false)
        {
            var workingDirectory = Path.Combine(this.defaultWorkingDirectory, "Down-" + DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss--fff"));
            byte[] ret;
            if (bundleAllDependencies)
            {
                var packageFilePaths = this.DownloadPackages(new[] { packageDescription }, workingDirectory, true);
                var bundleStagePath = Path.Combine(workingDirectory, "Bundle");
                foreach (var packageFilePath in packageFilePaths)
                {
                    var packageName = new FileInfo(packageFilePath).Name.Replace(".nupkg", string.Empty);
                    var targetPath = Path.Combine(bundleStagePath, packageName);
                    ZipFile.ExtractToDirectory(packageFilePath, targetPath);
                    
                    // thin out older frameworks so there is a single copy of the assembly (like if we have net45, net40, net35, windows8, etc. - only keep net45...).
                    var libPath = Path.Combine(targetPath, "lib");
                    var frameworkDirectories = Directory.Exists(libPath)
                                                   ? Directory.GetDirectories(libPath)
                                                   : new string[0];

                    if (frameworkDirectories.Any())
                    {
                        var frameworkFolderToKeep = frameworkDirectories.Length == 1
                                                        ? frameworkDirectories.Single()
                                                        : frameworkDirectories.Where(
                                                            directoryPath =>
                                                                {
                                                                    var directoryName = Path.GetFileName(directoryPath);
                                                                    var includeInWhere = directoryName != null
                                                                                         && directoryName.StartsWith(
                                                                                             "net",
                                                                                             StringComparison.InvariantCultureIgnoreCase);
                                                                    return includeInWhere;
                                                                }).OrderByDescending(_ => _).FirstOrDefault();

                        var unnecessaryFrameworks =
                            frameworkDirectories.Except(new[] { frameworkFolderToKeep }).ToList();
                        foreach (var unnecessaryFramework in unnecessaryFrameworks)
                        {
                            Directory.Delete(unnecessaryFramework, true);
                        }
                    }
                }

                var bundledFilePath = Path.Combine(workingDirectory, packageDescription.Id + "_DependenciesBundled.zip");
                ZipFile.CreateFromDirectory(bundleStagePath, bundledFilePath);
                ret = File.ReadAllBytes(bundledFilePath);
            }
            else
            {
                var packageFilePath = this.DownloadPackages(new[] { packageDescription }, workingDirectory).Single();
                ret = File.ReadAllBytes(packageFilePath);
            }

            // clean up temp files
            Directory.Delete(workingDirectory, true);

            return ret;
        }

        /// <inheritdoc />
        public Package GetPackage(PackageDescription packageDescription, bool bundleAllDependencies)
        {
            var ret = new Package
                          {
                              PackageDescription = packageDescription,
                              PackageFileBytes = this.GetPackageFile(packageDescription, bundleAllDependencies),
                              PackageFileBytesRetrievalDateTimeUtc = DateTime.UtcNow,
                          };

            return ret;
        }
    }

    public class CustomCredentialProvider : ICredentialProvider
    {
        private PackageRepositoryConfiguration repoConfig;

        public CustomCredentialProvider(PackageRepositoryConfiguration repoConfig)
        {
            this.repoConfig = repoConfig;
        }

        public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
        {
            var credentialCache = new CredentialCache();
            var uriPrefix = new Uri(this.repoConfig.Source);
            credentialCache.Add(
                uriPrefix,
                "Basic",
                new NetworkCredential(this.repoConfig.Username, this.repoConfig.Password));
            return credentialCache;
        }
    }
}
