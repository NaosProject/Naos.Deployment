// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestoreMongoDatabaseMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Scheduler
{
    using Naos.Database.Domain;
    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message to initiate a database restore on the server the handler is on.
    /// </summary>
    public class RestoreMongoDatabaseMessage : IMessage, IShareFilePath, IShareDatabaseName
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the database to restore to.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <inheritdoc />
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the replace to use.
        /// </summary>
        public ReplaceOption ReplaceOption { get; set; }
    }
}
