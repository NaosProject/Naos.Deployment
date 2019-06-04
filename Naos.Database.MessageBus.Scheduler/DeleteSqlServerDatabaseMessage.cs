// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeleteSqlServerDatabaseMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Scheduler
{
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message to delete a database on the server the handler is on.
    /// </summary>
    public class DeleteSqlServerDatabaseMessage : IMessage, IShareDatabaseName
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the database to delete.
        /// </summary>
        public string DatabaseName { get; set; }
    }
}
