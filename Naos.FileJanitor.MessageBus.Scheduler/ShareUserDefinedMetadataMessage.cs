// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareUserDefinedMetadataMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using System.Threading.Tasks;

    using Naos.FileJanitor.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message object to share a file path with remaining messages.
    /// </summary>
    public class ShareUserDefinedMetadataMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the path metadata to share with rest of sequence.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Has to be an array right now for sharing.")]
        public MetadataItem[] UserDefinedMetadataToShare { get; set; }
    }

    /// <summary>
    /// Message handler for <see cref="ShareFilePathMessage"/>.
    /// </summary>
    public class ShareUserDefinedMetadataMessageHandler : MessageHandlerBase<ShareUserDefinedMetadataMessage>, IShareUserDefinedMetadata
    {
        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(ShareUserDefinedMetadataMessage message)
        {
            this.UserDefinedMetadata = await Task.FromResult(message.UserDefinedMetadataToShare);
        }

        /// <inheritdoc />
        public MetadataItem[] UserDefinedMetadata { get; set; }
    }
}
