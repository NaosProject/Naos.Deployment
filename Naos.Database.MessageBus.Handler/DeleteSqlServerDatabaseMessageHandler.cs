// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeleteSqlServerDatabaseMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Handler
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;
    using Naos.Configuration.Domain;
    using Naos.Database.MessageBus.Scheduler;
    using Naos.Database.SqlServer.Administration;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.Validation.Recipes;

    /// <summary>
    /// Naos.MessageBus handler for RestoreMessages.
    /// </summary>
    public class DeleteSqlServerDatabaseMessageHandler : MessageHandlerBase<DeleteSqlServerDatabaseMessage>, IShareDatabaseName
    {
        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(DeleteSqlServerDatabaseMessage message)
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
            DeleteSqlServerDatabaseMessage message,
            DatabaseMessageHandlerSettings settings)
        {
            new { message }.Must().NotBeNull();
            new { settings }.Must().NotBeNull();

            using (var activity = Log.Enter(() => new { Message = message, DatabaseName = message.DatabaseName }))
            {
                {
                    // use this to avoid issues with database not there or going offline
                    var localhostConnection = settings.SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[message.DatabaseName.ToUpperInvariant()];
                    var masterConnectionString =
                        ConnectionStringHelper.SpecifyInitialCatalogInConnectionString(
                            localhostConnection.ToSqlServerConnectionString(),
                            SqlServerDatabaseManager.MasterDatabaseName);

                    var existingDatabases = SqlServerDatabaseManager.Retrieve(masterConnectionString);
                    if (existingDatabases.Any(_ => string.Equals(_.DatabaseName, message.DatabaseName, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        activity.Trace(() => "Deleting existing database before restore.");
                        SqlServerDatabaseManager.Delete(masterConnectionString, message.DatabaseName);
                    }
                    else
                    {
                        activity.Trace(() => "No existing database found to delete.");
                    }

                    this.DatabaseName = message.DatabaseName;

                    activity.Trace(() => "Completed successfully.");
                }
            }
        }

        /// <inheritdoc />
        public string DatabaseName { get; set; }
    }
}
