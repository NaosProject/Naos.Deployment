// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategySqlServer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Custom extension of the DeploymentConfiguration to accommodate database deployments.
    /// </summary>
    public class InitializationStrategySqlServer : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets restore information to apply prior to a migration, null will skip.
        /// </summary>
        public DatabaseRestoreBase Restore { get; set; }

        /// <summary>
        /// Gets or sets the database settings to use.
        /// </summary>
        public Create Create { get; set; }

        /// <summary>
        /// Gets or sets the schema/data migration information, null will skip.
        /// </summary>
        public DatabaseMigrationBase Migration { get; set; }

        /// <summary>
        /// Gets or sets the administrator password to use.
        /// </summary>
        public string AdministratorPassword { get; set; }

        /// <summary>
        /// Gets or sets the directory to save backup files in.
        /// </summary>
        public string BackupDirectory { get; set; }

        /// <summary>
        /// Gets or sets the directory to save data files in.
        /// </summary>
        public string DataDirectory { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategySqlServer
                          {
                              Name = this.Name,
                              DataDirectory = this.DataDirectory,
                              BackupDirectory = this.BackupDirectory,
                              AdministratorPassword = this.AdministratorPassword,
                              Create = (Create)this.Create.Clone(),
                              Restore = (DatabaseRestoreBase)this.Restore.Clone(),
                              Migration = (DatabaseMigrationBase)this.Migration.Clone()
                          };
            return ret;
        }
    }
}
