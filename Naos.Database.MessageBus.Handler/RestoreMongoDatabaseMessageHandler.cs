﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestoreMongoDatabaseMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Handler
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Naos.Configuration.Domain;
    using Naos.Database.MessageBus.Scheduler;
    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;
    using Naos.Mongo.Domain;
    using Naos.Mongo.Protocol.Client;
    using OBeautifulCode.Assertion.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Naos.MessageBus handler for RestoreMessages.
    /// </summary>
    public class RestoreMongoDatabaseMessageHandler : MessageHandlerBase<RestoreMongoDatabaseMessage>, IShareFilePath, IShareDatabaseName
    {
        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(RestoreMongoDatabaseMessage message)
        {
            if (!File.Exists(message.FilePath))
            {
                throw new FileNotFoundException("Could not find file to restore", message.FilePath);
            }

            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            await this.HandleAsync(message, settings);
        }

        /// <summary>
        /// Handles a RestoreMongoDatabaseMessage.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Needed settings to handle messages.</param>
        /// <returns>Task to support async await calling.</returns>
        public async Task HandleAsync(
            RestoreMongoDatabaseMessage message,
            DatabaseMessageHandlerSettings settings)
        {
            new { message }.AsArg().Must().NotBeNull();
            new { settings }.AsArg().Must().NotBeNull();

            using (var activity = Log.With(() => new { Message = message, message.DatabaseName, message.FilePath }))
            {
                {
                    this.DatabaseName = message.DatabaseName;
                    this.FilePath = message.FilePath;

                    var dataDirectory = settings.MongoDatabaseDataDirectory;
                    var dataFilePath = Path.Combine(dataDirectory, this.DatabaseName + "Dat.mdf");

                    var logFilePath = Path.Combine(dataDirectory, this.DatabaseName + "Log.ldf");

                    activity.Write(() => $"Using data path: {dataFilePath}, log path: {logFilePath}");

                    var restoreFilePath = new Uri(this.FilePath);
                    var restoreDetails = new RestoreMongoDatabaseDetails
                                             {
                                                 ReplaceOption = message.ReplaceOption,
                                                 RestoreFrom = restoreFilePath,
                                             };

                    activity.Write(() => Invariant($"Restoring Mongo database {this.DatabaseName} from {restoreFilePath}."));

                    var localhostConnection = settings.MongoDatabaseNameToLocalhostConnectionDefinitionMap[message.DatabaseName.ToUpperInvariant()];
                    await MongoDatabaseManager.RestoreFullAsync(
                        localhostConnection,
                        this.DatabaseName,
                        restoreDetails,
                        settings.WorkingDirectoryPath,
                        settings.MongoUtilityDirectory);

                    activity.Write(() => "Completed successfully.");
                }
            }
        }

        /// <inheritdoc />
        public string FilePath { get; set; }

        /// <inheritdoc />
        public string DatabaseName { get; set; }
    }
}
