// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareDatabaseNameMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Handler
{
    using System.Threading.Tasks;

    using Naos.Database.MessageBus.Scheduler;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Naos.MessageBus handler for Share.
    /// </summary>
    public class ShareDatabaseNameMessageHandler : MessageHandlerBase<ShareDatabaseNameMessage>, IShareDatabaseName
    {
        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(ShareDatabaseNameMessage message)
        {
            using (var log = Log.With(() => new { Message = message, DatabaseNameToShare = message.DatabaseNameToShare }))
            {
                log.Write(() => "Sharing database name: " + message.DatabaseNameToShare);
                this.DatabaseName = await Task.FromResult(message.DatabaseNameToShare);
            }
        }

        /// <inheritdoc />
        public string DatabaseName { get; set; }
    }
}
