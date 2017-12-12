// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageHelper.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
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
    using System.Text;

    using Its.Log.Instrumentation;

    using Naos.Packaging.Domain;

    using Spritely.Recipes;
    using Spritely.Redo;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        public PackageWithBundleIdentifier GetPackage(PackageDescription packageDescription, bool bundleAllDependencies)
        {
            new { packageDescription }.Must().NotBeNull().OrThrowFirstFailure();

            const string DirectoryDateTimeToStringFormat = "yyyy-MM-dd--HH-mm-ss--ffff";
            var localWorkingDirectory = Path.Combine(this.workingDirectory, "Down-" + DateTime.Now.ToString(DirectoryDateTimeToStringFormat, CultureInfo.CurrentCulture));

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
                var packageDownloadPaths = this.packageManager.DownloadPackages(new[] { packageDescription }, localWorkingDirectory);
                if (packageDownloadPaths.Count <= 0)
                {
                    throw new InvalidOperationException(FormattableString.Invariant($"Failed to get a package path for package description: {packageDescription.GetIdDotVersionString()} from PackageManager.DownloadPackages"));
                }

                var packageFilePath = packageDownloadPaths.Single();
                fileBytes = File.ReadAllBytes(packageFilePath);
            }

            // clean up temp files
            Using.LinearBackOff(TimeSpan.FromSeconds(5))
                .WithMaxRetries(3)
                .WithReporter(_ => Log.Write(new LogEntry(FormattableString.Invariant($"Retrying delete package working directory {localWorkingDirectory} due to error."), _)))
                .Run(() => Directory.Delete(localWorkingDirectory, true))
                .Now();

            var package = new Package
            {
                PackageDescription = packageDescription,
                PackageFileBytes = fileBytes,
                PackageFileBytesRetrievalDateTimeUtc = DateTime.UtcNow,
            };

            var ret = new PackageWithBundleIdentifier { Package = package, AreDependenciesBundled = bundleAllDependencies };

            return ret;
        }

        /// <summary>
        /// Finds directories for frameworks not being used and recommends ones to be removed.
        /// </summary>
        /// <param name="frameworkDirectories">List of paths for the supported frameworks.</param>
        /// <returns>List of paths deemed extraneous to be removed.</returns>
        public static IReadOnlyCollection<string> FindExtraneousFrameworksToDelete(string[] frameworkDirectories)
        {
            new { frameworkDirectories }.Must().NotBeNull().OrThrowFirstFailure();

            var directoriesToDelete = new List<string>();
            var frameworkFolderToKeep = frameworkDirectories.Length == 1
                                            ? frameworkDirectories.Single()
                                            : frameworkDirectories.Where(
                                                directoryPath =>
                                                {
                                                    var directoryName = Path.GetFileName(directoryPath);
                                                    var includeInWhere = directoryName != null
                                                                         && directoryName.StartsWith("net", StringComparison.OrdinalIgnoreCase)
                                                                         && !directoryName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase);
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

        /// <summary>
        /// Gets the specified version from the NUSPEC file in the package.
        /// </summary>
        /// <param name="package">Package to interrogate.</param>
        /// <returns>Version specified in the NUSPEC config.</returns>
        public string GetActualVersionFromPackage(Package package)
        {
            new { package }.Must().NotBeNull().OrThrowFirstFailure();

            if (string.Equals(
                package.PackageDescription.Id,
                PackageDescription.NullPackageId,
                StringComparison.CurrentCultureIgnoreCase))
            {
                return "[NULL PACKAGE VERSION IS IRRELEVANT]";
            }

            var nuspecSearchPattern = package.PackageDescription.Id + ".nuspec";
            var nuspecFileContents =
                this.packageManager.GetMultipleFileContentsFromPackageAsStrings(package, nuspecSearchPattern)
                    .Select(_ => _.Value)
                    .SingleOrDefault();
            var actualVersion = nuspecFileContents == null
                                    ? "[FAILED TO EXTRACT FROM PACKAGE]"
                                    : this.packageManager.GetVersionFromNuSpecFile(nuspecFileContents);
            return actualVersion;
        }

        /// <summary>
        /// Gets the contents of a file (as a string) matching the search pattern for the package in question (will decompress and search through the contents of the package).
        /// </summary>
        /// <param name="package">Package to find the file(s) in.</param>
        /// <param name="searchPattern">Infix pattern to use for searching for files.</param>
        /// <param name="encoding">Optional encoding to use (UTF-8 [no BOM] is default).</param>
        /// <returns>Dictionary of file name and contents of the file found as a string.</returns>
        public IDictionary<string, string> GetMultipleFileContentsFromPackageAsStrings(Package package, string searchPattern, Encoding encoding = null)
        {
            return this.packageManager.GetMultipleFileContentsFromPackageAsStrings(package, searchPattern, encoding);
        }

        /// <summary>
        /// Gets the contents of a file (as a string) matching the search pattern for the package in question (will decompress and search through the contents of the package).
        /// </summary>
        /// <param name="package">Package to find the file(s) in.</param>
        /// <param name="searchPattern">Infix pattern to use for searching for files.</param>
        /// <returns>Dictionary of file name and contents of the file found as a byte array.</returns>
        public IDictionary<string, byte[]> GetMultipleFileContentsFromPackageAsBytes(Package package, string searchPattern)
        {
            return this.packageManager.GetMultipleFileContentsFromPackageAsBytes(package, searchPattern);
        }
    }
}
