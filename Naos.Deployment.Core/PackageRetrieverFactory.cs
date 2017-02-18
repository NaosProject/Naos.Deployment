// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageRetrieverFactory.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;

    using Naos.Packaging.Domain;
    using Naos.Packaging.NuGet;

    /// <summary>
    /// Factory to build the package retriever to use with the deployment manager.
    /// </summary>
    public static class PackageRetrieverFactory
    {
        /// <summary>Creates a package retriever to use.</summary>
        /// <param name="repoConfig">Package repository configuration.</param>
        /// <param name="defaultWorkingDirectory">Working directory to download temporary files to.</param>
        /// <param name="consoleOutputCallback">Action to write console output to from package download process.</param>
        /// <returns>Package retriever to use.</returns>
        public static PackageRetriever BuildPackageRetriever(PackageRepositoryConfiguration repoConfig, string defaultWorkingDirectory, Action<string> consoleOutputCallback)
        {
            return new PackageRetriever(defaultWorkingDirectory, repoConfig, null, null, consoleOutputCallback);
        }
    }
}