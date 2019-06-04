// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestoreDirectoryMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using Naos.FileJanitor.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message object to restore a directory from an archive.
    /// </summary>
    public class RestoreDirectoryMessage : IMessage, IShareFilePath, IShareUserDefinedMetadata
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the source path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the file path to restore the archive to.
        /// </summary>
        public string TargetFilePath { get; set; }

        /// <summary>
        /// Gets or sets metadata to receive from sharing and add into.
        /// </summary>
        public MetadataItem[] UserDefinedMetadata { get; set; }
    }
}
