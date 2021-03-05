// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestoreSqlServerDatabaseMessageHandler.cs" company="Naos Project">
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
    using Naos.SqlServer.Domain;
    using Naos.SqlServer.Protocol.Client;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Database.Recipes;
    using static System.FormattableString;

    /// <summary>
    /// Naos.MessageBus handler for RestoreMessages.
    /// </summary>
    public class RestoreSqlServerDatabaseMessageHandler : MessageHandlerBase<RestoreSqlServerDatabaseMessage>, IShareFilePath, IShareDatabaseName
    {
        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(RestoreSqlServerDatabaseMessage message)
        {
            if (!File.Exists(message.FilePath))
            {
                throw new FileNotFoundException("Could not find file to restore", message.FilePath);
            }

            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            await this.HandleAsync(message, settings);
        }

        /// <summary>
        /// Handles a RestoreSqlServerDatabaseMessage.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Needed settings to handle messages.</param>
        /// <returns>Task to support async await calling.</returns>
        public async Task HandleAsync(
            RestoreSqlServerDatabaseMessage message,
            DatabaseMessageHandlerSettings settings)
        {
            new { message }.AsArg().Must().NotBeNull();
            new { settings }.AsArg().Must().NotBeNull();

            using (var activity = Log.With(() => new { Message = message, message.DatabaseName, message.FilePath }))
            {
                {
                    this.DatabaseName = message.DatabaseName;
                    this.FilePath = message.FilePath;

                    var dataDirectory = settings.SqlServerDatabaseDataDirectory;
                    var dataFilePath = Path.Combine(dataDirectory, this.DatabaseName + "Dat.mdf");

                    var logFilePath = Path.Combine(dataDirectory, this.DatabaseName + "Log.ldf");

                    activity.Write(() => $"Using data path: {dataFilePath}, log path: {logFilePath}");

                    var restoreFilePath = new Uri(this.FilePath);
                    var restoreDetails = new RestoreSqlServerDatabaseDetails(
                        dataFilePath,
                        logFilePath,
                        Device.Disk,
                        restoreFilePath,
                        null,
                        message.ChecksumOption,
                        message.ErrorHandling,
                        message.RecoveryOption,
                        message.ReplaceOption,
                        message.RestrictedUserOption);

                    activity.Write(() => Invariant($"Restoring SQL Server database {this.DatabaseName} from {restoreFilePath}."));

                    var localhostConnection = settings.SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[message.DatabaseName.ToUpperInvariant()];
                    // use this to avoid issues with database not there or going offline
                    var masterConnectionString = localhostConnection
                                                .BuildConnectionString(TimeSpan.FromSeconds(30))
                                                .AddOrUpdateInitialCatalogInConnectionString(SqlServerDatabaseManager.MasterDatabaseName);

                    await SqlServerDatabaseManager.RestoreFullAsync(
                        masterConnectionString,
                        this.DatabaseName,
                        restoreDetails,
                        null,
                        message.Timeout == default(TimeSpan) ? settings.DefaultTimeout : message.Timeout);

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
