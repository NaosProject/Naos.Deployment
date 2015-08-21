// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Package.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// A full package, description and the file bytes as of a date and time.
    /// </summary>
    public class Package
    {
        /// <summary>
        /// Gets or sets the description of the package.
        /// </summary>
        public PackageDescription PackageDescription { get; set; }

        /// <summary>
        /// Gets or sets the bytes of the package file at specified date and time.
        /// </summary>
        public byte[] PackageFileBytes { get; set; }

        /// <summary>
        /// Gets or sets the date and time UTC that the package file bytes were retrieved.
        /// </summary>
        public DateTime PackageFileBytesRetrievalDateTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the dependencies have been bundled with the specific package.
        /// </summary>
        public bool AreDependenciesBundled { get; set; }
    }
}
