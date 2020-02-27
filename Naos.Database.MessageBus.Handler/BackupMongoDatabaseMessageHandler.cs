// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackupMongoDatabaseMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Handler
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;
    using Naos.Configuration.Domain;
    using Naos.Database.MessageBus.Scheduler;
    using Naos.Database.Mongo;
    using Naos.Database.Mongo.Domain;
    using Naos.FileJanitor.Domain;
    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.Assertion.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Naos.MessageBus handler for BackupMessages.
    /// </summary>
    public class BackupMongoDatabaseMessageHandler : MessageHandlerBase<BackupMongoDatabaseMessage>, IShareFilePath, IShareDatabaseName, IShareUserDefinedMetadata
    {
        /// <inheritdoc />
        public override async Task HandleAsync(BackupMongoDatabaseMessage message)
        {
            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            await this.HandleAsync(message, settings);
        }

        /// <summary>
        /// Handles a BackupMongoDatabaseMessage.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Needed settings to handle messages.</param>
        /// <returns>Task to support async await calling.</returns>
        public async Task HandleAsync(BackupMongoDatabaseMessage message, DatabaseMessageHandlerSettings settings)
        {
            new { message }.AsArg().Must().NotBeNull();
            new { settings }.AsArg().Must().NotBeNull();

            using (var activity = Log.Enter(() => new { Message = message, DatabaseName = message.DatabaseName }))
            {
                // must have a date that is strictly alphanumeric...
                var datePart =
                    DateTime.UtcNow.ToString("u")
                        .Replace("-", string.Empty)
                        .Replace(":", string.Empty)
                        .Replace(" ", string.Empty);

                var backupDirectory = settings.MongoDatabaseBackupDirectory;
                var backupFilePath = Path.Combine(backupDirectory, message.BackupName) + "TakenOn" + datePart + ".bak";

                this.FilePath = backupFilePath;
                this.DatabaseName = message.DatabaseName;

                var backupFilePathUri = new Uri(this.FilePath);
                var backupDetails = new BackupMongoDatabaseDetails()
                                        {
                                            Name = message.BackupName,
                                            BackupTo = backupFilePathUri,
                                            Description = message.BackupDescription,
                                        };

                activity.Trace(() => Invariant($"Backing up Mongo database {this.DatabaseName} to {backupFilePath}."));

                var localhostConnection = settings.MongoDatabaseNameToLocalhostConnectionDefinitionMap[message.DatabaseName.ToUpperInvariant()];
                var archivedDirectory = await MongoDatabaseManager.BackupFullAsync(
                    localhostConnection,
                    this.DatabaseName,
                    backupDetails,
                    settings.WorkingDirectoryPath,
                    settings.MongoUtilityDirectory);

                this.UserDefinedMetadata = archivedDirectory.ToMetadataItemCollection().ToArray();

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
