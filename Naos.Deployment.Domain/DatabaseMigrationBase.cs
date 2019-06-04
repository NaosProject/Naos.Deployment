// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DatabaseMigrationBase.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Base class to describe a migration of a database.
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public abstract class DatabaseMigrationBase : ICloneable
    {
        /// <inheritdoc />
        public abstract object Clone();
    }

    /// <summary>
    /// Null object implementation for testing.
    /// </summary>
    public class NullDatabaseMigration : DatabaseMigrationBase
    {
        /// <inheritdoc />
        public override object Clone()
        {
            return new NullDatabaseMigration();
        }
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

        /// <inheritdoc />
        public override object Clone()
        {
            return new DatabaseMigrationFluentMigrator { Version = this.Version };
        }
    }
}