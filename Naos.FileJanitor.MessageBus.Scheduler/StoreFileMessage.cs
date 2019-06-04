// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StoreFileMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using System.Collections.Generic;
    using System.Security.Cryptography;

    using Naos.FileJanitor.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message object to store a file in centralized storage.
    /// </summary>
    public class StoreFileMessage : IMessage, IShareFilePath, IShareFileLocation, IShareUserDefinedMetadata
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the source path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the target location.
        /// </summary>
        public FileLocation FileLocation { get; set; }

        /// <summary>
        /// Gets or sets the hashing algorithms to compute, persist, and use in verification.
        /// </summary>
        public IReadOnlyCollection<string> HashingAlgorithmNames { get; set; }

        /// <summary>
        /// Gets or sets user defined meta data to save with the file.
        /// </summary>
        public MetadataItem[] UserDefinedMetadata { get; set; }
    }
}
