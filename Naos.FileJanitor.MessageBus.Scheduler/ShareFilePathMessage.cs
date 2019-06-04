// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareFilePathMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using System.Threading.Tasks;

    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message object to share a file path with remaining messages.
    /// </summary>
    public class ShareFilePathMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the path (in the context of the handling of the message) of file to share with rest of sequence.
        /// </summary>
        public string FilePathToShare { get; set; }
    }

    /// <summary>
    /// Message handler for <see cref="ShareFilePathMessage"/>.
    /// </summary>
    public class ShareFilePathMessageHandler : MessageHandlerBase<ShareFilePathMessage>, IShareFilePath
    {
        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(ShareFilePathMessage message)
        {
            this.FilePath = await Task.FromResult(message.FilePathToShare);
        }

        /// <inheritdoc />
        public string FilePath { get; set; }
    }
}
