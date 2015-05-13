// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManagePackages.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Methods to interface into the package repository.
    /// </summary>
    public interface IManagePackages
    {
        /// <summary>
        /// Gets the contents of a file matching the search pattern for the package in question.
        /// </summary>
        /// <param name="packageDescription">Description of the package to find the file in.</param>
        /// <param name="searchPattern">Infix pattern to use for searching for the file (should resolve to one file).</param>
        /// <returns>Contents of the file found (null if not found).</returns>
        string GetFileContentsFromPackage(PackageDescription packageDescription, string searchPattern);

        /// <summary>
        /// Downloads the specified package.
        /// </summary>
        /// <param name="packageDescription">Description of package to download.</param>
        /// <param name="workingDirectory">Directory to download and decompress package to.</param>
        void DownloadPackage(PackageDescription packageDescription, string workingDirectory);
    }
}
