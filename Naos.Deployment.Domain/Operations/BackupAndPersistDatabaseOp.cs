// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackupAndPersistDatabaseOp.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using Naos.SqlServer.Domain;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Type;

    /// <summary>
    /// Operation to backup a database and persist it to durable storage.
    /// </summary>
    public partial class BackupAndPersistDatabaseOp : VoidOperationBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackupAndPersistDatabaseOp"/> class.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="timeout">The timeout for the backup operation.</param>
        public BackupAndPersistDatabaseOp(
            string databaseName,
            TimeSpan timeout)
        {
            databaseName.MustForArg(nameof(databaseName)).NotBeNullNorWhiteSpace().And().BeAlphanumeric(SqlServerDatabaseDefinition.DatabaseNameAlphanumericOtherAllowedCharacters);

            this.DatabaseName = databaseName;
            this.Timeout = timeout;
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Gets the timeout for the backup operation.
        /// </summary>
        public TimeSpan Timeout { get; private set; }
    }
}
