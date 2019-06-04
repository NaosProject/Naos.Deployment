// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IShareDatabaseName.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Scheduler
{
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Interface to share database name with other messages in a sequence.
    /// </summary>
    public interface IShareDatabaseName : IShare
    {
        /// <summary>
        /// Gets or sets the name of the database to delete.
        /// </summary>
        string DatabaseName { get; set; }
    }
}
