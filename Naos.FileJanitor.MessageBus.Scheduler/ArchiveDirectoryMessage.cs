// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArchiveDirectoryMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using Naos.FileJanitor.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message object to archive a directory using specific kind to pass to factory.
    /// </summary>
    public class ArchiveDirectoryMessage : IMessage, IShareFilePath, IShareUserDefinedMetadata
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the source path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the file path to write the archive file to.
        /// </summary>
        public string TargetFilePath { get; set; }

        /// <summary>
        /// Gets or sets the archive kind.
        /// </summary>
        public DirectoryArchiveKind DirectoryArchiveKind { get; set; }

        /// <summary>
        /// Gets or sets the compression kind.
        /// </summary>
        public ArchiveCompressionKind ArchiveCompressionKind { get; set; }

        /// <summary>
        /// Gets or sets metadata to receive from sharing and add into.
        /// </summary>
        public MetadataItem[] UserDefinedMetadata { get; set; }
    }
}
