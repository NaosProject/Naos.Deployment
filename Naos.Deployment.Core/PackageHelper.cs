// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageHelper.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;
    using Naos.Recipes.RunWithRetry;

    /// <summary>
    /// Helper methods for using a package manager.
    /// </summary>
    public class PackageHelper
    {
        private readonly IGetPackages packageManager;
        private readonly IReadOnlyCollection<string> rootPackageDirectoriesToPrune;
        private readonly string workingDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageHelper"/> class.
        /// </summary>
        /// <param name="packageManager">Package manager to download packages.</param>
        /// <param name="rootPackageDirectoriesToPrune">Root directories to prune out of a package.</param>
        /// <param name="workingDirectory">Working directory to store files temporarily.</param>
        public PackageHelper(IGetPackages packageManager, IReadOnlyCollection<string> rootPackageDirectoriesToPrune, string workingDirectory)
        {
            this.packageManager = packageManager;
            this.rootPackageDirectoriesToPrune = rootPackageDirectoriesToPrune;
            this.workingDirectory = workingDirectory;
        }

        /// <summary>
        /// Gets a package with bundled dependencies option.
        /// </summary>
        /// <param name="packageDescription">Description of the package to download.</param>
        /// <param name="bundleAllDependencies">Whether or not to include dependencies in the bundle.</param>
        /// <returns>Package with indicator of bundling.</returns>
        public PackageWithBundleIdentifier GetPackage(PackageDescription packageDescription, bool bundleAllDependencies)
        {
            const string DirectoryDateTimeToStringFormat = "yyyy-MM-dd--HH-mm-ss--ffff";
            var localWorkingDirectory = Path.Combine(this.workingDirectory, "Down-" + DateTime.Now.ToString(DirectoryDateTimeToStringFormat));

            byte[] fileBytes;
            if (bundleAllDependencies)
            {
                var packageFilePaths = this.packageManager.DownloadPackages(new[] { packageDescription }, localWorkingDirectory, true);
                var bundleStagePath = Path.Combine(localWorkingDirectory, "Bundle");
                foreach (var packageFilePath in packageFilePaths)
                {
                    var packageName = new FileInfo(packageFilePath).Name.Replace(".nupkg", string.Empty);
                    var targetPath = Path.Combine(bundleStagePath, packageName);
                    ZipFile.ExtractToDirectory(packageFilePath, targetPath);
                    var directoriesToDelete = new List<string>();

                    foreach (var directoryToTrim in this.rootPackageDirectoriesToPrune)
                    {
                        var fullPathDirectoryToTrim = Path.Combine(targetPath, directoryToTrim);
                        directoriesToDelete.Add(fullPathDirectoryToTrim);
                    }

                    // thin out older frameworks so there is a single copy of the assembly (like if we have net45, net40, net35, windows8, etc. - only keep newest...).
                    var libPath = Path.Combine(targetPath, "lib");
                    var frameworkDirectories = Directory.Exists(libPath) ? Directory.GetDirectories(libPath) : new string[0];

                    if (frameworkDirectories.Any())
                    {
                        var additionalDirectoriesToDelete = PackageHelper.FindExtraneousFrameworksToDelete(frameworkDirectories);
                        directoriesToDelete.AddRange(additionalDirectoriesToDelete);
                    }

                    foreach (var directoryToDelete in directoriesToDelete)
                    {
                        if (Directory.Exists(directoryToDelete))
                        {
                            Directory.Delete(directoryToDelete, true);
                        }
                    }
                }

                var bundledFilePath = Path.Combine(localWorkingDirectory, packageDescription.Id + "_DependenciesBundled.zip");
                ZipFile.CreateFromDirectory(bundleStagePath, bundledFilePath);
                fileBytes = File.ReadAllBytes(bundledFilePath);
            }
            else
            {
                var packageFilePath = this.packageManager.DownloadPackages(new[] { packageDescription }, localWorkingDirectory).Single();
                fileBytes = File.ReadAllBytes(packageFilePath);
            }

            // clean up temp files
            Retry.RunAsync(() => Task.Run(() => Directory.Delete(localWorkingDirectory, true))).Wait();

            var package = new Package
            {
                PackageDescription = packageDescription,
                PackageFileBytes = fileBytes,
                PackageFileBytesRetrievalDateTimeUtc = DateTime.UtcNow
            };

            var ret = new PackageWithBundleIdentifier { Package = package, AreDependenciesBundled = bundleAllDependencies };

            return ret;
        }

        /// <summary>
        /// Finds directories for frameworks not being used and recommends ones to be removed.
        /// </summary>
        /// <param name="frameworkDirectories">List of paths for the supported frameworks.</param>
        /// <returns>List of paths deemed extraneous to be removed.</returns>
        public static List<string> FindExtraneousFrameworksToDelete(string[] frameworkDirectories)
        {
            var directoriesToDelete = new List<string>();
            var frameworkFolderToKeep = frameworkDirectories.Length == 1
                                            ? frameworkDirectories.Single()
                                            : frameworkDirectories.Where(
                                                directoryPath =>
                                                {
                                                    var directoryName = Path.GetFileName(directoryPath);
                                                    var includeInWhere = directoryName != null
                                                                         && directoryName.StartsWith("net", StringComparison.InvariantCultureIgnoreCase)
                                                                         && !directoryName.StartsWith("netstandard", StringComparison.InvariantCultureIgnoreCase);
                                                    return includeInWhere;
                                                }).OrderByDescending(_ => _).FirstOrDefault();

            // ReSharper disable once ConvertIfStatementToNullCoalescingExpression - seems more confusing that way...
            if (frameworkFolderToKeep == null)
            {
                // this will happen with a package that doesn't honor the 'NET' prefix on framework folders...
                frameworkFolderToKeep = frameworkDirectories.Where(
                    directoryPath =>
                    {
                        var directoryName = Path.GetFileName(directoryPath);
                        var includeInWhere = directoryName != null;
                        return includeInWhere;
                    }).OrderByDescending(_ => _).FirstOrDefault();
            }

            var unnecessaryFrameworks = frameworkDirectories.Except(new[] { frameworkFolderToKeep }).ToList();
            directoriesToDelete.AddRange(unnecessaryFrameworks);

            return directoriesToDelete;
        }
    }
}
