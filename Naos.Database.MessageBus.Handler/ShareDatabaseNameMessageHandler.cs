// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareDatabaseNameMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Handler
{
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;

    using Naos.Database.MessageBus.Scheduler;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Naos.MessageBus handler for Share.
    /// </summary>
    public class ShareDatabaseNameMessageHandler : MessageHandlerBase<ShareDatabaseNameMessage>, IShareDatabaseName
    {
        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(ShareDatabaseNameMessage message)
        {
            using (var log = Log.Enter(() => new { Message = message, DatabaseNameToShare = message.DatabaseNameToShare }))
            {
                log.Trace(() => "Sharing database name: " + message.DatabaseNameToShare);
                this.DatabaseName = await Task.FromResult(message.DatabaseNameToShare);
            }
        }

        /// <inheritdoc />
        public string DatabaseName { get; set; }
    }
}
