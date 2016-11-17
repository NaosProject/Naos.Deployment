// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.SqlServer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Naos.Database.Contract;
    using Naos.Database.Migrator;
    using Naos.Database.Tools;
    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private List<SetupStep> GetSqlServerSpecificSteps(InitializationStrategySqlServer sqlServerStrategy, Package package)
        {
            if (sqlServerStrategy.Create != null && sqlServerStrategy.Restore != null)
            {
                throw new NotSupportedException(
                    "A create and restore on a single database initialization strategy is not supported.");
            }

            var databaseSteps = new List<SetupStep>();
            var sqlServiceAccount = this.settings.DatabaseServerSettings.SqlServiceAccount;

            var backupDirectory = sqlServerStrategy.BackupDirectory ?? this.settings.DatabaseServerSettings.DefaultBackupDirectory;
            var createBackupDirScript = this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createBackupDirParams = new[] { backupDirectory, sqlServiceAccount };
            databaseSteps.Add(
                new SetupStep 
                    {
                        Description = "Create " + backupDirectory + " and grant rights to SQL service account.",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(createBackupDirScript.ScriptText, createBackupDirParams)
                    });
            var backupProcessAccount = this.settings.DatabaseServerSettings.BackupProcessServiceAccount;
            var addBackupProcessAclsToBackupDirScript = this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var addBackupProcessAclsToBackupDirParams = new[] { backupDirectory, backupProcessAccount };
            databaseSteps.Add(
                new SetupStep 
                    {
                        Description = "Add rights to " + backupDirectory + " for backup process account.",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(addBackupProcessAclsToBackupDirScript.ScriptText, addBackupProcessAclsToBackupDirParams)
                    });

            var dataDirectory = sqlServerStrategy.DataDirectory ?? this.settings.DatabaseServerSettings.DefaultDataDirectory;
            var createDatabaseDirScript = this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createDatabaseDirParams = new[] { dataDirectory, sqlServiceAccount };
            databaseSteps.Add(
                new SetupStep 
                {
                    Description = "Create " + dataDirectory + " and grant rights to SQL service account.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(createDatabaseDirScript.ScriptText, createDatabaseDirParams)
                });

            var enableSaSetPasswordScript = this.settings.DeploymentScriptBlocks.EnableSaAccountAndSetPassword;
            var enableSaSetPasswordParams = new[] { sqlServerStrategy.AdministratorPassword };
            databaseSteps.Add(
                new SetupStep 
                {
                    Description = "Turn on Mixed Mode Auth, enable SA account, and set password.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(enableSaSetPasswordScript.ScriptText, enableSaSetPasswordParams)
                });

            var updateDefaultInstancePathsScript = this.settings.DeploymentScriptBlocks.SetDefaultDirectories;
            var updateDefaultInstancePathsParams = new[] { dataDirectory, dataDirectory, backupDirectory };

            databaseSteps.Add(
                new SetupStep
                {
                    Description = "Update default instance paths on database.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(updateDefaultInstancePathsScript.ScriptText, updateDefaultInstancePathsParams)
                });

            var restartSqlServerScript = this.settings.DeploymentScriptBlocks.RestartWindowsService;
            var restartSqlServerParams = new[] { this.settings.DatabaseServerSettings.SqlServiceName };
            databaseSteps.Add(
                new SetupStep
                {
                    Description = "Restart SQL server for account change(s) to take effect.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(restartSqlServerScript.ScriptText, restartSqlServerParams)
                });

            var connectionString = "Server=localhost; user id=sa; password=" + sqlServerStrategy.AdministratorPassword;
            var databaseConfigurationForCreation = this.BuildDatabaseConfiguration(
                sqlServerStrategy.Name,
                dataDirectory,
                sqlServerStrategy.RecoveryMode,
                sqlServerStrategy.Create == null ? null : sqlServerStrategy.Create.DatabaseFileNameSettings,
                sqlServerStrategy.Create == null ? null : sqlServerStrategy.Create.DatabaseFileSizeSettings);

            databaseSteps.Add(
                new SetupStep
                    {
                        Description = "Create database: " + sqlServerStrategy.Name,
                        SetupFunc = machineManager =>
                            {
                                var realRemoteConnectionString = connectionString.Replace("localhost", machineManager.IpAddress);
                                DatabaseManager.Create(realRemoteConnectionString, databaseConfigurationForCreation);
                                return new dynamic[0];
                            }
                    });

            if (sqlServerStrategy.Restore != null)
            {
                var awsRestore = sqlServerStrategy.Restore as DatabaseRestoreFromS3;
                if (awsRestore == null)
                {
                    throw new NotSupportedException(
                        "Currently no support for type of database restore: " + sqlServerStrategy.Restore.GetType());
                }

                var databaseConfigurationForRestore = this.BuildDatabaseConfiguration(
                    sqlServerStrategy.Name,
                    dataDirectory,
                    sqlServerStrategy.RecoveryMode,
                sqlServerStrategy.Restore == null ? null : sqlServerStrategy.Restore.DatabaseFileNameSettings,
                sqlServerStrategy.Restore == null ? null : sqlServerStrategy.Restore.DatabaseFileSizeSettings);
                databaseSteps.Add(
                    new SetupStep
                        {
                            Description = $"Restore - Region: {awsRestore.Region}; Bucket: {awsRestore.BucketName}; File: {awsRestore.FileName}",
                            SetupFunc = machineManager =>
                                {
                                    var restoreFilePath = Path.Combine(sqlServerStrategy.BackupDirectory, awsRestore.FileName);

                                    var remoteDownloadBackupScriptBlock = this.settings.DeploymentScriptBlocks.DownloadS3Object.ScriptText;
                                    var remoteDownloadBackupScriptParams = new[]
                                                                               {
                                                                                   awsRestore.BucketName, awsRestore.FileName, restoreFilePath,
                                                                                   awsRestore.Region, awsRestore.DownloadAccessKey,
                                                                                   awsRestore.DownloadSecretKey
                                                                               };

                                    machineManager.RunScript(remoteDownloadBackupScriptBlock, remoteDownloadBackupScriptParams);
                                    var realRemoteConnectionString = connectionString.Replace("localhost", machineManager.IpAddress);

                                    var restoreFileUri = new Uri(restoreFilePath);
                                    var checksumOption = awsRestore.RunChecksum ? ChecksumOption.Checksum : ChecksumOption.NoChecksum;
                                    var restoreDetails = new RestoreDetails
                                                             {
                                                                 ChecksumOption = checksumOption,
                                                                 Device = Device.Disk,
                                                                 ErrorHandling = ErrorHandling.StopOnError,
                                                                 DataFilePath = databaseConfigurationForRestore.DataFilePath,
                                                                 LogFilePath = databaseConfigurationForRestore.LogFilePath,
                                                                 RecoveryOption = RecoveryOption.NoRecovery,
                                                                 ReplaceOption = ReplaceOption.ReplaceExistingDatabase,
                                                                 RestoreFrom = restoreFileUri,
                                                                 RestrictedUserOption = RestrictedUserOption.Normal
                                                             };
                                    DatabaseManager.RestoreFull(realRemoteConnectionString, sqlServerStrategy.Name, restoreDetails);
                                    return new dynamic[0];
                                }
                        });
            }

            if (sqlServerStrategy.Migration != null)
            {
                var fluentMigration = sqlServerStrategy.Migration as DatabaseMigrationFluentMigrator;
                if (fluentMigration == null)
                {
                    throw new NotSupportedException(
                        "Currently no support for type of database migration: " + sqlServerStrategy.Migration.GetType());
                }

                databaseSteps.Add(
                    new SetupStep
                        {
                            Description = "Run Database Fluent Migration to Version: " + fluentMigration.Version,
                            SetupFunc = machineManager =>
                                {
                                    var realRemoteConnectionString = connectionString.Replace("localhost", machineManager.IpAddress);
                                    var migrationDllBytes =
                                        this.packageManager.GetMultipleFileContentsFromPackageAsBytes(package, package.PackageDescription.Id + ".dll")
                                            .Select(_ => _.Value)
                                            .SingleOrDefault();
                                    var migrationPdbBytes =
                                        this.packageManager.GetMultipleFileContentsFromPackageAsBytes(package, package.PackageDescription.Id + ".pdb")
                                            .Select(_ => _.Value)
                                            .SingleOrDefault();

                                    var assembly = migrationPdbBytes == null
                                                       ? Assembly.Load(migrationDllBytes)
                                                       : Assembly.Load(migrationDllBytes, migrationPdbBytes);

                                    MigrationExecutor.Up(assembly, realRemoteConnectionString, sqlServerStrategy.Name, fluentMigration.Version);

                                    return new dynamic[0];
                                }
                        });
            }

            return databaseSteps;
        }

        private DatabaseConfiguration BuildDatabaseConfiguration(string databaseName, string dataDirectory, string recoveryMode, DatabaseFileNameSettings databaseFileNameSettings, DatabaseFileSizeSettings databaseFileSizeSettings)
        {
            var localDatabaseFileNameSettings = databaseFileNameSettings
                                                ?? new DatabaseFileNameSettings
                                                {
                                                    DataFileLogicalName = databaseName + "Dat",
                                                    DataFileNameOnDisk = databaseName + ".mdf",
                                                    LogFileLogicalName = databaseName + "Log",
                                                    LogFileNameOnDisk = databaseName + ".log"
                                                };

            var localDatabaseFileSizeSettings = databaseFileSizeSettings
                                                ?? this.settings.DefaultDatabaseFileSizeSettings;

            var recoveryModeEnum = RecoveryMode.Unspecified;
            if (!string.IsNullOrEmpty(recoveryMode))
            {
                recoveryModeEnum = (RecoveryMode)Enum.Parse(typeof(RecoveryMode), recoveryMode, true);
            }

            var databaseConfiguration = new DatabaseConfiguration
            {
                DatabaseName = databaseName,
                DatabaseType = DatabaseType.User,
                RecoveryMode = recoveryModeEnum,
                DataFileLogicalName = localDatabaseFileNameSettings.DataFileLogicalName,
                DataFilePath = Path.Combine(dataDirectory, localDatabaseFileNameSettings.DataFileNameOnDisk),
                DataFileCurrentSizeInKb = localDatabaseFileSizeSettings.DataFileCurrentSizeInKb,
                DataFileMaxSizeInKb = localDatabaseFileSizeSettings.DataFileMaxSizeInKb,
                DataFileGrowthSizeInKb = localDatabaseFileSizeSettings.DataFileGrowthSizeInKb,
                LogFileLogicalName = localDatabaseFileNameSettings.LogFileLogicalName,
                LogFilePath = Path.Combine(dataDirectory, localDatabaseFileNameSettings.LogFileNameOnDisk),
                LogFileCurrentSizeInKb = localDatabaseFileSizeSettings.LogFileCurrentSizeInKb,
                LogFileMaxSizeInKb = localDatabaseFileSizeSettings.LogFileMaxSizeInKb,
                LogFileGrowthSizeInKb = localDatabaseFileSizeSettings.LogFileGrowthSizeInKb
            };

            return databaseConfiguration;
        }
    }
}
