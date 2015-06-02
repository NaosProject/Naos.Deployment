// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyDatabase.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Custom extension of the DeploymentConfiguration to accommodate database deployments.
    /// </summary>
    public class InitializationStrategyDatabase : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the migration number to run the migration up to.
        /// </summary>
        public long? MigrationNumber { get; set; }
    }
}
