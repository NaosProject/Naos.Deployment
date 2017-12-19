// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategySqlServer.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using static System.FormattableString;

    /// <summary>
    /// Custom extension of the DeploymentConfiguration to accommodate database deployments.
    /// </summary>
    public class InitializationStrategySqlServer : InitializationStrategyBase, IHaveManagementChannel
    {
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the instance if desired to override; DEFAULT will be used if not specified but can specify "SQLEXPRESS", etc.
        /// </summary>
        public string InstanceName { get; set; }

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

        /// <summary>
        /// Gets or sets the recovery mode to use for the database.
        /// </summary>
        public string RecoveryMode { get; set; }

        /// <summary>
        /// Gets or sets the channel name to monitor management commands.
        /// </summary>
        public string ManagementChannelName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to download all dependant packages and re-package them together.
        /// This will make targeting something for execution impossible and should not be used in conjuction
        /// with <see cref="InitializationStrategyScheduledTask" /> or <see cref="InitializationStrategyOnetimeCall" />.
        /// </summary>
        public bool BundleDependencies { get; set; }

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
                              Migration = (DatabaseMigrationBase)this.Migration.Clone(),
                              ManagementChannelName = this.ManagementChannelName,
                          };
            return ret;
        }

        /// <summary>
        /// Builds a localhost connection string from the configuration.
        /// </summary>
        /// <returns>Localhost connection string.</returns>
        public string CreateLocalhostConnectionString()
        {
            var instanceName = string.IsNullOrWhiteSpace(this.InstanceName) ? string.Empty : Invariant($"\\{this.InstanceName}");
            var ret = Invariant($"Server=localhost{instanceName}; user id=sa; password={this.AdministratorPassword}");
            return ret;
        }
    }
}
