// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CopySqlServerDatabaseObjectMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Database.MessageBus.Scheduler
{
    using System.Collections.Generic;

    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message to copy objects from one database to another.
    /// </summary>
    public class CopySqlServerDatabaseObjectMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the database copy objects from.
        /// </summary>
        public string SourceDatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the name of the database copy objects to.
        /// </summary>
        public string TargetDatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the object names to copy in order to copy.
        /// </summary>
        public IReadOnlyList<string> OrderedObjectNamesToCopy { get; set; }
    }
}
