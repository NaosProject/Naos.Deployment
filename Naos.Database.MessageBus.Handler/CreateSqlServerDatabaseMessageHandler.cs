// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateSqlServerDatabaseMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Handler
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Configuration.Domain;
    using Naos.Database.MessageBus.Scheduler;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;
    using Naos.SqlServer.Domain;
    using Naos.SqlServer.Protocol.Client;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Database.Recipes;

    /// <summary>
    /// Naos.MessageBus handler for CreateSqlServerDatabaseMessages.
    /// </summary>
    public class CreateSqlServerDatabaseMessageHandler : MessageHandlerBase<CreateSqlServerDatabaseMessage>, IShareDatabaseName
    {
        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(CreateSqlServerDatabaseMessage message)
        {
            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            await Task.Run(() => this.Handle(message, settings));
        }

        /// <summary>
        /// Handles a RestoreDatabaseMessage.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Needed settings to handle messages.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Keeping, seems reasonable.")]
        public void Handle(
            CreateSqlServerDatabaseMessage message,
            DatabaseMessageHandlerSettings settings)
        {
            new { message }.AsArg().Must().NotBeNull();
            new { settings }.AsArg().Must().NotBeNull();

            using (var activity = Log.With(() => new { Message = message, DatabaseName = message.DatabaseName }))
            {
                {
                    // use this to avoid issues with database not there or going offline
                    var localhostConnection = settings.SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[message.DatabaseName.ToUpperInvariant()];
                    var masterConnectionString =
                        localhostConnection
                           .BuildConnectionString(TimeSpan.FromSeconds(30))
                           .AddOrUpdateInitialCatalogInConnectionString(SqlServerDatabaseManager.MasterDatabaseName);

                    var existingDatabases = SqlServerDatabaseManager.Retrieve(masterConnectionString);
                    if (existingDatabases.Any(_ => string.Equals(_.DatabaseName, message.DatabaseName, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        throw new ArgumentException("Cannot create a database because it's already present, please delete first.");
                    }

                    var dataDirectory = settings.SqlServerDatabaseDataDirectory;
                    var dataFilePath = Path.Combine(dataDirectory, message.DataFileName);
                    var logFilePath = Path.Combine(dataDirectory, message.DataFileName);

                    var databaseConfiguration = new DatabaseConfiguration(
                        message.DatabaseName,
                        message.DatabaseType,
                        RecoveryMode.Simple,
                        message.DataFileLogicalName,
                        dataFilePath,
                        message.DataFileCurrentSizeInKb,
                        message.DataFileMaxSizeInKb,
                        message.DataFileGrowthSizeInKb,
                        message.LogFileLogicalName,
                        logFilePath,
                        message.LogFileCurrentSizeInKb,
                        message.LogFileMaxSizeInKb,
                        message.LogFileGrowthSizeInKb);

                    SqlServerDatabaseManager.Create(masterConnectionString, databaseConfiguration);

                    this.DatabaseName = message.DatabaseName;

                    activity.Write(() => "Completed successfully.");
                }
            }
        }

        /// <inheritdoc />
        public string DatabaseName { get; set; }
    }
}
