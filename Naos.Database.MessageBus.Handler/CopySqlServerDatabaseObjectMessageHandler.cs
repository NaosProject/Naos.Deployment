// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CopySqlServerDatabaseObjectMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Handler
{
    using System;
    using System.Threading.Tasks;

    using Naos.Configuration.Domain;
    using Naos.Database.MessageBus.Scheduler;
    using Naos.MessageBus.Domain;
    using Naos.SqlServer.Protocol.Client;
    using Naos.SqlServer.Protocol.Management;
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

            var sourceDatabaseConnectionString = settings
                                                .SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[message.SourceDatabaseName.ToUpperInvariant()]
                                                .BuildConnectionString(TimeSpan.FromSeconds(30));

            var targetDatabaseConnectionString = settings
                                                .SqlServerDatabaseNameToLocalhostConnectionDefinitionMap[message.TargetDatabaseName.ToUpperInvariant()]
                                                .BuildConnectionString(TimeSpan.FromSeconds(30));

            await DatabaseObjectCopier.CopyObjects(message.OrderedObjectNamesToCopy, sourceDatabaseConnectionString, targetDatabaseConnectionString);
        }
    }
}
