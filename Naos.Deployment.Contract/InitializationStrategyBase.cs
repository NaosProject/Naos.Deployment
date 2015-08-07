// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyBase.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Strategy to initialize the application.
    /// </summary>
    [KnownType(typeof(InitializationStrategyMessageBusHandler))]
    [KnownType(typeof(InitializationStrategyDatabase))]
    [KnownType(typeof(InitializationStrategyWeb))]
    public abstract class InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets DNS entries to be applied to the private IP address of the created instance.
        /// </summary>
        public ICollection<string> PrivateDnsEntries { get; set; }

        /// <summary>
        /// Gets or sets certificates to install on instance.
        /// </summary>
        public ICollection<string> CertificatesToInstall { get; set; }

        /// <summary>
        /// Gets or sets directories to create on file system.
        /// </summary>
        public ICollection<DirectoryToCreateDetails> DirectoriesToCreate { get; set; }
    }
}
