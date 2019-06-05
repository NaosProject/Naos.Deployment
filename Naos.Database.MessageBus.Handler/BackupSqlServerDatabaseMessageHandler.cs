// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackupSqlServerDatabaseMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Handler
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;
    using Naos.Configuration.Domain;
    using Naos.Database.MessageBus.Scheduler;
    using Naos.Database.SqlServer.Administration;
    using Naos.Database.SqlServer.Domain;
    using Naos.FileJanitor.Domain;
    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.Validation.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Naos.MessageBus handler for BackupMessages.
    /// </summary>
    public class BackupSqlServerDatabaseMessageHandler : MessageHandlerBase<BackupSqlServerDatabaseMessage>, IShareFilePath, IShareDatabaseName, IShareUserDefinedMetadata
    {
        /// <inheritdoc />
        public override async Task HandleAsync(BackupSqlServerDatabaseMessage message)
        {
            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            await this.HandleAsync(message, settings);
        }

        /// <summary>
        /// Handles a BackupSqlServerDatabaseMessage.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Needed settings to handle messages.</param>
        /// <returns>Task to support async await calling.</returns>
        public async Task HandleAsync(BackupSqlServerDatabaseMessage message, DatabaseMessageHandlerSettings settings)
        {
            new { message }.Must().NotBeNull();
            new { settings }.Must().NotBeNull();

            using (var activity = Log.Enter(() => new { Message = message, DatabaseName = message.DatabaseName }))
            {
                // must have a date that is strictly alphanumeric...
                var datePart =
                    DateTime.UtcNow.ToString("u")
                        .Replace("-", string.Empty)
                        .Replace(":", string.Empty)
                        .Replace(" ", string.Empty);

                var backupDirectory = settings.SqlServerDatabaseBackupDirectory;
                var backupFilePath = Path.Combine(backupDirectory, message.BackupName) + "TakenOn" + datePart + ".bak";

                this.FilePath = backupFilePath;
                this.DatabaseName = message.DatabaseName;

                var backupFilePathUri = new Uri(this.FilePath);
                var backupDetails = new BackupSqlServerDatabaseDetails
                                        {
                                            Name = message.BackupName,
                                            BackupTo = backupFilePathUri,
                                            ChecksumOption = message.ChecksumOption,
                                            Cipher = message.Cipher,
                                            CompressionOption = message.CompressionOption,
                                            Description = message.BackupDescription,
                                            Device = Device.Disk,
                                            ErrorHandling = message.ErrorHandling,
                                        };

                activity.Trace(() => Invariant($"Backing up SQL Server database {this.DatabaseName} to {backupFilePath}."));

                var localhostConnection = settings.SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[message.DatabaseName.ToUpperInvariant()];
                await SqlServerDatabaseManager.BackupFullAsync(
                    localhostConnection.ToSqlServerConnectionString(),
                    this.DatabaseName,
                    backupDetails,
                    null,
                    message.Timeout == default(TimeSpan) ? settings.DefaultTimeout : message.Timeout);
                this.UserDefinedMetadata = new MetadataItem[0];

                activity.Trace(() => "Completed successfully.");
            }
        }

        /// <inheritdoc />
        public string FilePath { get; set; }

        /// <inheritdoc />
        public string DatabaseName { get; set; }

        /// <inheritdoc />
        public MetadataItem[] UserDefinedMetadata { get; set; }
    }
}
