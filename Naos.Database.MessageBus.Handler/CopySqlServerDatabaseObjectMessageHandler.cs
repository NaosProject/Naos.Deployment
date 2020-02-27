// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CopySqlServerDatabaseObjectMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Handler
{
    using System.Threading.Tasks;

    using Naos.Configuration.Domain;
    using Naos.Database.MessageBus.Scheduler;
    using Naos.Database.SqlServer.Administration;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.Assertion.Recipes;

    /// <summary>
    /// Naos.MessageBus handler for Share.
    /// </summary>
    public class CopySqlServerDatabaseObjectMessageHandler : MessageHandlerBase<CopySqlServerDatabaseObjectMessage>
    {
        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(CopySqlServerDatabaseObjectMessage message)
        {
            new { message }.AsArg().Must().NotBeNull();

            var settings = Config.Get<DatabaseMessageHandlerSettings>();
            new { settings }.AsArg().Must().NotBeNull();

            var sourceDatabaseConnectionString = settings.SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[message.SourceDatabaseName.ToUpperInvariant()].ToSqlServerConnectionString();
            var targetDatabaseConnectionString = settings.SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[message.TargetDatabaseName.ToUpperInvariant()].ToSqlServerConnectionString();
            await DatabaseObjectCopier.CopyObjects(message.OrderedObjectNamesToCopy, sourceDatabaseConnectionString, targetDatabaseConnectionString);
        }
    }
}
