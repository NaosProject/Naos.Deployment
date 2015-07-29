// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManagePackages.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Methods to interface into the package repository.
    /// </summary>
    public interface IManagePackages
    {
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

        /// <summary>
        /// Gets the contents of a file (as a string) matching the search pattern for the package in question (will decompress and search through the contents of the package).
        /// </summary>
        /// <param name="package">Package to find the file(s) in.</param>
        /// <param name="searchPattern">Infix pattern to use for searching for files.</param>
        /// <param name="encoding">Optional encoding to use (Unicode is default).</param>
        /// <returns>Dictionary of file name and contents of the file found as a string.</returns>
        IDictionary<string, string> GetMultipleFileContentsFromPackageAsStrings(Package package, string searchPattern, Encoding encoding = null);

        /// <summary>
        /// Gets the contents of a file (as a string) matching the search pattern for the package in question (will decompress and search through the contents of the package).
        /// </summary>
        /// <param name="package">Package to find the file(s) in.</param>
        /// <param name="searchPattern">Infix pattern to use for searching for files.</param>
        /// <returns>Dictionary of file name and contents of the file found as a byte array.</returns>
        IDictionary<string, byte[]> GetMultipleFileContentsFromPackageAsBytes(Package package, string searchPattern);
    }
}
