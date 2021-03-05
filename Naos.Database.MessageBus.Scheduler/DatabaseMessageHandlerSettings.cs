// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DatabaseMessageHandlerSettings.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Scheduler
{
    using System;
    using System.Collections.Generic;

    using Naos.Mongo.Domain;
    using Naos.SqlServer.Domain;

    /// <summary>
    /// Model object for Its.Configuration providing settings for the MessageHandlers.
    /// </summary>
    public class DatabaseMessageHandlerSettings
    {
        /// <summary>
        /// Gets or sets the default timeout for use on database operations.
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; }

        /// <summary>
        /// Gets or sets the working directory to use for intermediate storage.
        /// </summary>
        public string WorkingDirectoryPath { get; set; }

        /// <summary>
        /// Gets or sets the directory where Mongo utilies are.
        /// </summary>
        public string MongoUtilityDirectory { get; set; }

        /// <summary>
        /// Gets or sets a map of the database name to a <see cref="SqlServerConnectionDefinition" /> to use for local host database operations.
        /// </summary>
        public IReadOnlyDictionary<string, SqlServerConnectionDefinition> SqlServerDatabaseNameToLocalhostConnectionDefinitionMap { get; set; }

        /// <summary>
        /// Gets or sets a map of the database name to a <see cref="MongoConnectionDefinition" /> to use for local host database operations.
        /// </summary>
        public IReadOnlyDictionary<string, MongoConnectionDefinition> MongoDatabaseNameToLocalhostConnectionDefinitionMap { get; set; }

        /// <summary>
        /// Gets or sets the SQL Server location on disk for data.
        /// </summary>
        public string SqlServerDatabaseDataDirectory { get; set; }

        /// <summary>
        /// Gets or sets the Mongo location on disk for data.
        /// </summary>
        public string MongoDatabaseDataDirectory { get; set; }

        /// <summary>
        /// Gets or sets the SQL Server location on disk for backups.
        /// </summary>
        public string SqlServerDatabaseBackupDirectory { get; set; }

        /// <summary>
        /// Gets or sets the Mongo location on disk for backups.
        /// </summary>
        public string MongoDatabaseBackupDirectory { get; set; }
    }
}
