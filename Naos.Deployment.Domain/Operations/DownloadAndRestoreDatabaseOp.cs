// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloadAndRestoreDatabaseOp.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using Naos.FileJanitor.Domain;
    using Naos.SqlServer.Domain;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Type;

    /// <summary>
    /// Operation to find a database backup on durable storage and restore it to the server.
    /// </summary>
    public partial class DownloadAndRestoreDatabaseOp : VoidOperationBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadAndRestoreDatabaseOp"/> class.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="timeout">The timeout for the restore operation.</param>
        /// <param name="keyPrefixSearchPattern">The key prefix search pattern to use on durable storage.</param>
        /// <param name="multipleKeysFoundStrategy">The multiple keys found strategy to use during search.</param>
        public DownloadAndRestoreDatabaseOp(
            string databaseName,
            TimeSpan timeout,
            string keyPrefixSearchPattern,
            MultipleKeysFoundStrategy multipleKeysFoundStrategy)
        {
            databaseName.MustForArg(nameof(databaseName)).NotBeNullNorWhiteSpace().And().BeAlphanumeric(SqlServerDatabaseDefinition.DatabaseNameAlphanumericOtherAllowedCharacters);

            this.DatabaseName = databaseName;
            this.Timeout = timeout;
            this.KeyPrefixSearchPattern = keyPrefixSearchPattern;
            this.MultipleKeysFoundStrategy = multipleKeysFoundStrategy;
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Gets the timeout for the restore operation.
        /// </summary>
        public TimeSpan Timeout { get; private set; }

        /// <summary>
        /// Gets the key prefix search pattern to use on durable storage.
        /// </summary>
        public string KeyPrefixSearchPattern { get; private set; }

        /// <summary>
        /// Gets the multiple keys found strategy to use during search.
        /// </summary>
        public MultipleKeysFoundStrategy MultipleKeysFoundStrategy { get; private set; }
    }
}
