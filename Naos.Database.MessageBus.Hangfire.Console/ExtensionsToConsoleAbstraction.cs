// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionsToConsoleAbstraction.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Hangfire.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using CLAP;

    using Naos.Configuration.Domain;
    using Naos.Database.Domain;
    using Naos.Database.MessageBus.Scheduler;
    using Naos.Logging.Domain;
    using Naos.Mongo.Domain;
    using Naos.Mongo.Protocol.Client;
    using Naos.Recipes.RunWithRetry;
    using Naos.SqlServer.Domain;
    using Naos.SqlServer.Protocol.Client;
    using static System.FormattableString;

    /// <summary>
    /// Abstraction for use with <see cref="CLAP" /> to provide basic command line interaction.
    /// </summary>
    public partial class ConsoleAbstraction
    {
        /// <summary>
        /// Backup a database to a file.
        /// </summary>
        /// <param name="databaseName">Name of database.</param>
        /// <param name="targetFilePath">Path to create backup at.</param>
        /// <param name="environment">Sets the Its.Configuration precedence to use specific settings.</param>
        /// <param name="debug">Launches the debugger.</param>
        [Verb(Aliases = "backupsql", Description = "Backup MS SQL Server database.")]
        public static void BackupSqlDatabase(
            [Required] [Aliases("name")] [Description("Name of database.")] string databaseName,
            [Required] [Aliases("file")] [Description("Path to create back at.")] string targetFilePath,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug)
        {
            // Only do console logging since these are intended to be additions to the primary functionality of processing messages from a queue
            CommonSetup(
                debug,
                environment,
                new LogWritingSettings(
                    new[]
                    {
                        new ConsoleLogConfig(
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.String, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                { LogItemKind.Object, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                            },
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.Exception, null },
                            }),
                    }));

            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            var connectionDefinition = settings.SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[databaseName.ToUpperInvariant()];
            var connectionString = connectionDefinition
               .BuildConnectionString(TimeSpan.FromSeconds(30));

            var errorHandling = ErrorHandling.StopOnError;
            var compressionOption = CompressionOption.NoCompression;

            var backupFilePathUri = new Uri(targetFilePath);
            var backupDetails = new BackupSqlServerDatabaseDetails(
                Invariant($"{databaseName}DatabaseBackup"),
                null,
                Device.Disk,
                backupFilePathUri,
                null,
                compressionOption,
                ChecksumOption.NoChecksum,
                errorHandling,
                Cipher.NoEncryption,
                Encryptor.None,
                null);

            Run.TaskUntilCompletion(SqlServerDatabaseManager.BackupFullAsync(connectionString, databaseName, backupDetails));
        }

        /// <summary>
        /// Backup a database to a file.
        /// </summary>
        /// <param name="databaseName">Name of database.</param>
        /// <param name="targetFilePath">Path to create backup at.</param>
        /// <param name="utilityPath">Path to find supporting utilities (only needed for Mongo kind - should have mongodump.exe and mongorestore.exe).</param>
        /// <param name="workingDirectory">Path to write temp file (DEFAULT is parent of targetFilePath).</param>
        /// <param name="environment">Sets the Its.Configuration precedence to use specific settings.</param>
        /// <param name="debug">Launches the debugger.</param>
        [Verb(Aliases = "backupmongo", Description = "Backup Mongo database.")]
        public static void BackupMongoDatabase(
            [Required] [Aliases("name")] [Description("Name of database.")] string databaseName,
            [Required] [Aliases("file")] [Description("Path to create back at.")] string targetFilePath,
            [Aliases("utility")] [Description("Path to find supporting utilities (should have mongodump.exe & mongorestore.exe).")] string utilityPath,
            [Aliases("temp")] [Description("Path to write temp file (DEFAULT is parent of targetFilePath).")] string workingDirectory,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug)
        {
            // Only do console logging since these are intended to be additions to the primary functionality of processing messages from a queue
            CommonSetup(
                debug,
                environment,
                new LogWritingSettings(
                    new[]
                    {
                        new ConsoleLogConfig(
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.String, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                { LogItemKind.Object, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                            },
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.Exception, null },
                            }),
                    }));

            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            var connectionDefinition = settings.MongoDatabaseNameToLocalhostConnectionDefinitionMap[databaseName.ToUpperInvariant()];

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = Path.GetDirectoryName(targetFilePath);
            }

            var backupFilePathUri = new Uri(targetFilePath);
            var backupDetails = new BackupMongoDatabaseDetails
            {
                Name = Invariant($"{databaseName}DatabaseBackup"),
                BackupTo = backupFilePathUri,
                Description = null,
            };

            Run.TaskUntilCompletion(MongoDatabaseManager.BackupFullAsync(connectionDefinition, databaseName, backupDetails, workingDirectory, utilityPath));
        }

        /// <summary>
        /// Restore a database from a file.
        /// </summary>
        /// <param name="databaseName">Name of database.</param>
        /// <param name="sourceFilePath">Path to create back at.</param>
        /// <param name="dataDirectory">Directory housing data and log files.</param>
        /// <param name="environment">Sets the Its.Configuration precedence to use specific settings.</param>
        /// <param name="debug">Launches the debugger.</param>
        [Verb(Aliases = "restoresql", Description = "Restore MS SQL Server database.")]
        public static void RestoreSqlDatabase(
            [Required] [Aliases("name")] [Description("Name of database.")] string databaseName,
            [Required] [Aliases("file")] [Description("Path to load backup from.")] string sourceFilePath,
            [Required] [Aliases("data")] [Description("Directory housing data and log files.")] string dataDirectory,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug)
        {
            // Only do console logging since these are intended to be additions to the primary functionality of processing messages from a queue
            CommonSetup(
                debug,
                environment,
                new LogWritingSettings(
                    new[]
                    {
                        new ConsoleLogConfig(
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.String, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                { LogItemKind.Object, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                            },
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.Exception, null },
                            }),
                    }));

            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            var connectionDefinition = settings.SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[databaseName.ToUpperInvariant()];
            var connectionString = connectionDefinition
               .BuildConnectionString(TimeSpan.FromSeconds(30));

            var dataFilePath = Path.Combine(dataDirectory, databaseName + "Dat.mdf");
            var logFilePath = Path.Combine(dataDirectory, databaseName + "Log.ldf");

            var errorHandling = ErrorHandling.StopOnError;
            var recoveryOption = RecoveryOption.Recovery;

            var backupFilePathUri = new Uri(sourceFilePath);
            var restoreDetails = new RestoreSqlServerDatabaseDetails(
                dataFilePath,
                logFilePath,
                Device.Disk,
                backupFilePathUri,
                null,
                ChecksumOption.NoChecksum,
                errorHandling,
                recoveryOption,
                ReplaceOption.ReplaceExistingDatabase,
                RestrictedUserOption.Normal);

            Run.TaskUntilCompletion(SqlServerDatabaseManager.RestoreFullAsync(connectionString, databaseName, restoreDetails));
        }

        /// <summary>
        /// Restore a database from a file.
        /// </summary>
        /// <param name="databaseName">Name of database.</param>
        /// <param name="sourceFilePath">Path to create back at.</param>
        /// <param name="utilityPath">Path to find supporting utilities (only needed for Mongo kind - should have mongodump.exe and mongorestore.exe).</param>
        /// <param name="workingDirectory">Path to write temp file (DEFAULT is parent of targetFilePath).</param>
        /// <param name="environment">Sets the Its.Configuration precedence to use specific settings.</param>
        /// <param name="debug">Launches the debugger.</param>
        [Verb(Aliases = "restoremongo", Description = "Restore Mongo database.")]
        public static void RestoreMongoDatabase(
            [Required] [Aliases("name")] [Description("Name of database.")] string databaseName,
            [Required] [Aliases("file")] [Description("Path to load backup from.")] string sourceFilePath,
            [Aliases("utility")] [Description("Path to find supporting utilities (should have mongodump.exe & mongorestore.exe).")] string utilityPath,
            [Aliases("temp")] [Description("Path to write temp file (DEFAULT is parent of sourceFilePath).")] string workingDirectory,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug)
        {
            // Only do console logging since these are intended to be additions to the primary functionality of processing messages from a queue
            CommonSetup(
                debug,
                environment,
                new LogWritingSettings(
                    new[]
                    {
                        new ConsoleLogConfig(
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.String, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                { LogItemKind.Object, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                            },
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.Exception, null },
                            }),
                    }));

            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            var connectionDefinition = settings.MongoDatabaseNameToLocalhostConnectionDefinitionMap[databaseName.ToUpperInvariant()];

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = Path.GetDirectoryName(sourceFilePath);
            }

            var backupFilePathUri = new Uri(sourceFilePath);
            var restoreDetails = new RestoreMongoDatabaseDetails
            {
                ReplaceOption = ReplaceOption.ReplaceExistingDatabase,
                RestoreFrom = backupFilePathUri,
            };

            Run.TaskUntilCompletion(MongoDatabaseManager.RestoreFullAsync(connectionDefinition, databaseName, restoreDetails, workingDirectory, utilityPath));
        }
    }
}
