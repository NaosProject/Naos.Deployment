// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DatabaseMigrationBase.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Base class to describe a migration of a database.
    /// </summary>
    [KnownType(typeof(DatabaseMigrationFluentMigrator))]
    public class DatabaseMigrationBase
    {
    }

    /// <summary>
    /// Implementation of a database migration using fluent migrator.
    /// </summary>
    public class DatabaseMigrationFluentMigrator : DatabaseMigrationBase
    {
        /// <summary>
        /// Gets or sets the version of the migration to run to.
        /// </summary>
        public long Version { get; set; }
    }
}