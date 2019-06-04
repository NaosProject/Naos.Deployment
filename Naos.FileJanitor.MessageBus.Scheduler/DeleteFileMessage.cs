// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeleteFileMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message object to delete a file (created to support removing temp files after they've been moved to backup storage).
    /// </summary>
    public class DeleteFileMessage : IMessage, IShareFilePath
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the path (in the context of the handling of the message) of file to delete.
        /// </summary>
        public string FilePath { get; set; }
    }
}
