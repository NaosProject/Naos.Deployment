// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackupSqlServerDatabaseMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Scheduler
{
    using System;
    using Naos.MessageBus.Domain;
    using Naos.SqlServer.Domain;

    /// <summary>
    /// Message to initiate a database backup on the server the handler is on.
    /// </summary>
    public class BackupSqlServerDatabaseMessage : IMessage, IShareDatabaseName
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the database to backup.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the name to use on a backup.
        /// </summary>
        public string BackupName { get; set; }

        /// <summary>
        /// Gets or sets the description to use on a backup.
        /// </summary>
        public string BackupDescription { get; set; }

        /// <summary>
        /// Gets or sets the checksum option to use.
        /// </summary>
        public ChecksumOption ChecksumOption { get; set; }

        /// <summary>
        /// Gets or sets the error handling to use.
        /// </summary>
        public ErrorHandling ErrorHandling { get; set; }

        /// <summary>
        /// Gets or sets the cipher to use.
        /// </summary>
        public Cipher Cipher { get; set; }

        /// <summary>
        /// Gets or sets the compression option to use.
        /// </summary>
        public CompressionOption CompressionOption { get; set; }

        /// <summary>
        /// Gets or sets an optional timeout; if not specified then the <see cref="DatabaseMessageHandlerSettings.DefaultTimeout" /> will be used.
        /// </summary>
        public TimeSpan Timeout { get; set; }
    }
}
