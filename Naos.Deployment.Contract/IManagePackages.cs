// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManagePackages.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    /// <summary>
    /// Methods to interface into the package repository.
    /// </summary>
    public interface IManagePackages
    {
        /// <summary>
        /// Gets the contents of a file matching the search pattern for the package in question (will decompress and search through the contents of the package).
        /// </summary>
        /// <param name="package">Package to find the file in.</param>
        /// <param name="searchPattern">Infix pattern to use for searching for the file (should resolve to one file).</param>
        /// <returns>Contents of the file found (null if not found).</returns>
        string GetFileContentsFromPackage(Package package, string searchPattern);

        /// <summary>
        /// Downloads the specified packages.
        /// </summary>
        /// <param name="packageDescriptions">Description of packages to download.</param>
        /// <param name="workingDirectory">Directory to download and decompress package to.</param>
        /// <param name="includeDependencies">Include dependencies when downloading (default is FALSE).</param>
        /// <returns>Full paths to the files that were downloaded.</returns>
        ICollection<string> DownloadPackages(ICollection<PackageDescription> packageDescriptions, string workingDirectory, bool includeDependencies = false);

        /// <summary>
        /// Gets package file for a package description.
        /// </summary>
        /// <param name="packageDescription">Package description to get file for.</param>
        /// <param name="bundleAllDependencies">Bundle all dependant assemblies into the package file (default is FALSE).</param>
        /// <returns>Package (description and file).</returns>
        Package GetPackage(PackageDescription packageDescription, bool bundleAllDependencies = false);
    }
}
