// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.SqlServer.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Naos.Database.Domain;
    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;
    using Naos.SqlServer.Domain;
    using Naos.SqlServer.Protocol.Client;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Reflection.Recipes;
    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Like it this way.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom", Justification = "This pattern works better for correctly loading dependencies.")]
        private List<SetupStep> GetSqlServerSpecificSteps(InitializationStrategySqlServer sqlServerStrategy, Package package)
        {
            if (sqlServerStrategy.Create != null && sqlServerStrategy.Restore != null)
            {
                throw new NotSupportedException(Invariant($"A create and restore on a single database initialization strategy is not supported, see config for '{package.PackageDescription.Id}'."));
            }

            var databaseSteps = new List<SetupStep>();
            var sqlServiceAccount = this.Settings.DatabaseServerSettings.SqlServiceAccount;

            var backupDirectory = sqlServerStrategy.BackupDirectory ?? this.Settings.DatabaseServerSettings.DefaultBackupDirectory;
            var createBackupDirScript = this.Settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createBackupDirParams = new[] { backupDirectory, sqlServiceAccount };
            databaseSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"Create {backupDirectory} and grant rights to SQL service account for '{package.PackageDescription.Id}'."),
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(createBackupDirScript.ScriptText, createBackupDirParams).ToList(),
                    });
            var backupProcessAccount = this.Settings.DatabaseServerSettings.BackupProcessServiceAccount;
            var addBackupProcessAclsToBackupDirScript = this.Settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var addBackupProcessAclsToBackupDirParams = new[] { backupDirectory, backupProcessAccount };
            databaseSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"Add rights to {backupDirectory} for backup process account for '{package.PackageDescription.Id}'."),
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(addBackupProcessAclsToBackupDirScript.ScriptText, addBackupProcessAclsToBackupDirParams).ToList(),
                    });

            var dataDirectory = sqlServerStrategy.DataDirectory ?? this.Settings.DatabaseServerSettings.DefaultDataDirectory;
            var createDatabaseDirScript = this.Settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createDatabaseDirParams = new[] { dataDirectory, sqlServiceAccount };
            databaseSteps.Add(
                new SetupStep
                {
                    Description = Invariant($"Create {dataDirectory} and grant rights to SQL service account for '{package.PackageDescription.Id}'."),
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(createDatabaseDirScript.ScriptText, createDatabaseDirParams).ToList(),
                });

            var enableSaSetPasswordScript = this.Settings.DeploymentScriptBlocks.EnableSaAccountAndSetPassword;
            var enableSaSetPasswordParams = new[] { sqlServerStrategy.AdministratorPassword };
            databaseSteps.Add(
                new SetupStep
                {
                    Description = Invariant($"Turn on Mixed Mode Auth, enable SA account, and set password for '{package.PackageDescription.Id}'."),
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(enableSaSetPasswordScript.ScriptText, enableSaSetPasswordParams).ToList(),
                });

            var updateDefaultInstancePathsScript = this.Settings.DeploymentScriptBlocks.SetDefaultDirectories;
            var updateDefaultInstancePathsParams = new[] { dataDirectory, dataDirectory, backupDirectory };

            databaseSteps.Add(
                new SetupStep
                {
                    Description = Invariant($"Update default instance paths on database for '{package.PackageDescription.Id}'."),
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(updateDefaultInstancePathsScript.ScriptText, updateDefaultInstancePathsParams).ToList(),
                });

            var restartSqlServerScript = this.Settings.DeploymentScriptBlocks.RestartWindowsService;
            var restartSqlServerParams = new[] { this.Settings.DatabaseServerSettings.SqlServiceName };
            databaseSteps.Add(
                new SetupStep
                {
                    Description = Invariant($"Restart SQL server for account change(s) to take effect for '{package.PackageDescription.Id}'."),
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(restartSqlServerScript.ScriptText, restartSqlServerParams).ToList(),
                });

            var connectionString = sqlServerStrategy.CreateLocalhostConnectionString();
            var databaseConfigurationForCreation = this.BuildDatabaseConfiguration(
                sqlServerStrategy.Name,
                dataDirectory,
                sqlServerStrategy.RecoveryMode,
                sqlServerStrategy.Create?.DatabaseFileNameSettings,
                sqlServerStrategy.Create?.DatabaseFileSizeSettings);

            if (!sqlServerStrategy.DatabaseExists)
            {
                databaseSteps.Add(
                    new SetupStep
                    {
                        Description = Invariant($"Create database: {sqlServerStrategy.Name} on instance {sqlServerStrategy.InstanceName ?? "DEFAULT"} for '{package.PackageDescription.Id}'."),
                        SetupFunc = machineManager =>
                        {
                            var realRemoteConnectionString = connectionString.Replace("localhost", machineManager.Address);
                            SqlServerDatabaseManager.Create(realRemoteConnectionString, databaseConfigurationForCreation);
                            return new dynamic[0];
                        },
                    });
            }

            if (sqlServerStrategy.Restore != null)
            {
                if (!(sqlServerStrategy.Restore is DatabaseRestoreFromS3 awsRestore))
                {
                    throw new NotSupportedException(Invariant($"Currently no support for type of database restore '{sqlServerStrategy.Restore.GetType()}' for '{package.PackageDescription.Id}'."));
                }

                var databaseConfigurationForRestore = this.BuildDatabaseConfiguration(
                    sqlServerStrategy.Name,
                    dataDirectory,
                    sqlServerStrategy.RecoveryMode,
                    sqlServerStrategy.Restore?.DatabaseFileNameSettings,
                    sqlServerStrategy.Restore?.DatabaseFileSizeSettings);

                databaseSteps.Add(
                    new SetupStep
                        {
                            Description = Invariant($"Restore - Region: {awsRestore.Region}; Bucket: {awsRestore.BucketName}; File: {awsRestore.FileName}."),
                            SetupFunc = machineManager =>
                                {
                                    var restoreFilePath = Path.Combine(sqlServerStrategy.BackupDirectory, awsRestore.FileName);

                                    var remoteDownloadBackupScriptBlock = this.Settings.DeploymentScriptBlocks.DownloadS3Object.ScriptText;
                                    var remoteDownloadBackupScriptParams = new[]
                                                                               {
                                                                                   awsRestore.BucketName, awsRestore.FileName, restoreFilePath, awsRestore.Region,
                                                                                   awsRestore.DownloadAccessKey, awsRestore.DownloadSecretKey,
                                                                               };

                                    machineManager.RunScript(remoteDownloadBackupScriptBlock, remoteDownloadBackupScriptParams);
                                    var realRemoteConnectionString = connectionString.Replace("localhost", machineManager.Address);

                                    var restoreFileUri = new Uri(restoreFilePath);
                                    var checksumOption = awsRestore.RunChecksum ? ChecksumOption.Checksum : ChecksumOption.NoChecksum;

                                    var restoreDetails = new RestoreSqlServerDatabaseDetails(
                                        databaseConfigurationForRestore.DataFilePath,
                                        databaseConfigurationForRestore.LogFilePath,
                                        Device.Disk,
                                        restoreFileUri,
                                        null,
                                        checksumOption,
                                        ErrorHandling.StopOnError,
                                        RecoveryOption.NoRecovery,
                                        ReplaceOption.ReplaceExistingDatabase,
                                        RestrictedUserOption.Normal);

                                    SqlServerDatabaseManager.RestoreFull(realRemoteConnectionString, sqlServerStrategy.Name, restoreDetails);
                                    return new dynamic[0];
                                },
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
                                                    LogFileNameOnDisk = databaseName + ".log",
                                                };

            var localDatabaseFileSizeSettings = databaseFileSizeSettings
                                                ?? this.Settings.DefaultDatabaseFileSizeSettings;

            var recoveryModeEnum = RecoveryMode.Unspecified;
            if (!string.IsNullOrEmpty(recoveryMode))
            {
                recoveryModeEnum = (RecoveryMode)Enum.Parse(typeof(RecoveryMode), recoveryMode, true);
            }

            var databaseConfiguration = new DatabaseConfiguration(
                databaseName,
                DatabaseType.User,
                recoveryModeEnum,
                localDatabaseFileNameSettings.DataFileLogicalName,
                Path.Combine(dataDirectory, localDatabaseFileNameSettings.DataFileNameOnDisk),
                localDatabaseFileSizeSettings.DataFileCurrentSizeInKb,
                localDatabaseFileSizeSettings.DataFileMaxSizeInKb,
                localDatabaseFileSizeSettings.DataFileGrowthSizeInKb,
                localDatabaseFileNameSettings.LogFileLogicalName,
                Path.Combine(dataDirectory, localDatabaseFileNameSettings.LogFileNameOnDisk),
                localDatabaseFileSizeSettings.LogFileCurrentSizeInKb,
                localDatabaseFileSizeSettings.LogFileMaxSizeInKb,
                localDatabaseFileSizeSettings.LogFileGrowthSizeInKb);

            return databaseConfiguration;
        }
    }
}
