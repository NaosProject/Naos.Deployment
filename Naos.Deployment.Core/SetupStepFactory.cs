// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.cs" company="Naos">
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
    using System.Text;

    using Naos.Database.Migrator;
    using Naos.Database.Tools;
    using Naos.Database.Tools.Backup;
    using Naos.Deployment.Contract;
    
    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public class SetupStepFactory
    {
        /// <summary>
        /// Root path that all packages are deployed to.
        /// </summary>
        public const string RootDeploymentPath = @"D:\Deployments\";

        private readonly IGetCertificates certificateManager;

        private readonly SetupStepFactorySettings settings;

        private readonly IManagePackages packageManager;

        public SetupStepFactory(SetupStepFactorySettings settings, IGetCertificates certificateManager, IManagePackages packageManager)
        {
            this.certificateManager = certificateManager;
            this.settings = settings;
            this.packageManager = packageManager;
        }

        /// <summary>
        /// Get the appropriate setup steps for the packaged config.
        /// </summary>
        /// <param name="packagedConfig">Config to base setup steps from.</param>
        /// <param name="environment">Environment that is being deployed.</param>
        /// <returns>Collection of setup steps that will leave the machine properly configured.</returns>
        public ICollection<SetupStep> GetSetupSteps(PackagedDeploymentConfiguration packagedConfig, string environment)
        {
            var ret = new List<SetupStep>();

            var installChocoStep = this.GetChocolateySetupStep(packagedConfig);
            if (installChocoStep != null)
            {
                ret.Add(installChocoStep);
            }

            // don't include the package push for databases only since they will be deployed remotely...
            if (packagedConfig.GetInitializationStrategiesOf<InitializationStrategyDatabase>().Count()
                != packagedConfig.InitializationStrategies.Count)
            {
                var deployUnzippedFileStep = this.GetCopyAndUnzipPackageStep(packagedConfig);
                ret.Add(deployUnzippedFileStep);
            }

            foreach (var initializationStrategy in packagedConfig.InitializationStrategies)
            {
                var initSteps = this.GetStrategySpecificSetupSteps(initializationStrategy, packagedConfig, environment);
                ret.AddRange(initSteps);
            }

            return ret;
        }

        private ICollection<SetupStep> GetStrategySpecificSetupSteps(InitializationStrategyBase strategy, PackagedDeploymentConfiguration packagedConfig, string environment)
        {
            var ret = new List<SetupStep>();
            var packageDirectoryPath = GetPackageDirectoryPath(packagedConfig);

            if (strategy.GetType() == typeof(InitializationStrategyWeb))
            {
                var webRootPath = Path.Combine(packageDirectoryPath, "packagedWebsite"); // this needs to match how the package was built in the build system...
                var webSteps = this.GetWebSpecificSetupSteps(
                    (InitializationStrategyWeb)strategy,
                    packagedConfig.ItsConfigOverrides,
                    packageDirectoryPath,
                    webRootPath,
                    environment);
                ret.AddRange(webSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyDatabase))
            {
                var databaseSteps = this.GetDatabaseSpecificSteps((InitializationStrategyDatabase)strategy, packagedConfig.Package);
                ret.AddRange(databaseSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyMessageBusHandler))
            {
                /* No additional steps necessary as the DeploymentManager should have included a harness by virtue of this type of initialization strategy */
            }
            else
            {
                throw new DeploymentException("The initialization strategy type is not supported: " + strategy.GetType());
            }

            return ret;
        }

        private List<SetupStep> GetWebSpecificSetupSteps(InitializationStrategyWeb webStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string packageDirectoryPath, string webRootPath, string environment)
        {
            var webSteps = new List<SetupStep>();

            var webConfigPath = Path.Combine(webRootPath, "web.config");
            var updateWebConfigScriptBlock = this.settings.DeploymentScriptBlocks.UpdateItsConfigPrecedence;
            var updateWebConfigScriptParams = new[] { webConfigPath, environment };

            webSteps.Add(
                new SetupStep
                    {
                        Description = "Update Its.Config precedence: " + environment,
                        SetupAction =
                            machineManager =>
                            machineManager.RunScript(
                                updateWebConfigScriptBlock.ScriptText,
                                updateWebConfigScriptParams)
                    });

            foreach (var itsConfigOverride in itsConfigOverrides ?? new List<ItsConfigOverride>())
            {
                var itsFileSubPath = string.Format(
                    ".config/{0}/{1}.json",
                    environment,
                    itsConfigOverride.FileNameWithoutExtension);

                var itsFilePath = Path.Combine(webRootPath, itsFileSubPath);
                var itsFileBytes = Encoding.ASCII.GetBytes(itsConfigOverride.FileContentsJson);

                webSteps.Add(
                    new SetupStep 
                        {
                            Description =
                                "(Over)write Its.Config file: " + itsConfigOverride.FileNameWithoutExtension,
                            SetupAction =
                                machineManager => machineManager.SendFile(itsFilePath, itsFileBytes)
                        });
            }

            var certDetails = this.certificateManager.GetCertificateByName(webStrategy.SslCertificateName);
            if (certDetails == null)
            {
                throw new DeploymentException("Could not find certificate by name: " + webStrategy.SslCertificateName);
            }

            var certificateTargetPath = Path.Combine(packageDirectoryPath, certDetails.GenerateFileName());
            var appPoolStartMode = webStrategy.AppPoolStartMode == ApplicationPoolStartMode.None
                                       ? ApplicationPoolStartMode.OnDemand
                                       : webStrategy.AppPoolStartMode;

            var autoStartProviderName = webStrategy.AutoStartProvider == null
                                            ? null
                                            : webStrategy.AutoStartProvider.Name;
            var autoStartProviderType = webStrategy.AutoStartProvider == null
                                            ? null
                                            : webStrategy.AutoStartProvider.Type;

            const bool EnableSni = false;
            const bool AddHostHeaders = true;
            var installWebParameters = new object[]
                                           {
                                               webRootPath, webStrategy.PrimaryDns, certificateTargetPath,
                                               certDetails.CertificatePassword, appPoolStartMode, autoStartProviderName,
                                               autoStartProviderType, EnableSni, AddHostHeaders
                                           };

            webSteps.Add(
                new SetupStep 
                    {
                        Description = "Send certificate file (removed after installation): " + certDetails.GenerateFileName(),
                        SetupAction =
                            machineManager => machineManager.SendFile(certificateTargetPath, certDetails.FileBytes)
                    });

            webSteps.Add(
                new SetupStep 
                    {
                        Description = "Install IIS and configure website/webservice (this could take several minutes).",
                        SetupAction =
                            machineManager =>
                            machineManager.RunScript(this.settings.DeploymentScriptBlocks.InstallAndConfigureWebsite.ScriptText, installWebParameters)
                    });

            return webSteps;
        }

        private List<SetupStep> GetDatabaseSpecificSteps(InitializationStrategyDatabase databaseStrategy, Package package)
        {
            var databaseSteps = new List<SetupStep>();
            var sqlServiceAccount = this.settings.DatabaseServerSettings.SqlServiceAccount;

            var backupDirectory = databaseStrategy.BackupDirectory ?? this.settings.DatabaseServerSettings.DefaultBackupDirectory;
            var createBackupDirScript = this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createBackupDirParams = new[] { backupDirectory, sqlServiceAccount };
            databaseSteps.Add(
                new SetupStep 
                    {
                        Description = "Create " + backupDirectory + " and grant rights to SQL service account.",
                        SetupAction =
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
                        SetupAction =
                            machineManager =>
                            machineManager.RunScript(addBackupProcessAclsToBackupDirScript.ScriptText, addBackupProcessAclsToBackupDirParams)
                    });

            var dataDirectory = databaseStrategy.DataDirectory ?? this.settings.DatabaseServerSettings.DefaultDataDirectory;
            var createDatabaseDirScript = this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createDatabaseDirParams = new[] { dataDirectory, sqlServiceAccount };
            databaseSteps.Add(
                new SetupStep 
                {
                    Description = "Create " + dataDirectory + " and grant rights to SQL service account.",
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(createDatabaseDirScript.ScriptText, createDatabaseDirParams)
                });

            var enableSaSetPasswordScript = this.settings.DeploymentScriptBlocks.EnableSaAccountAndSetPassword;
            var enableSaSetPasswordParams = new[] { databaseStrategy.AdministratorPassword };
            databaseSteps.Add(
                new SetupStep 
                {
                    Description = "Turn on Mixed Mode Auth, enable SA account, and set password.",
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(enableSaSetPasswordScript.ScriptText, enableSaSetPasswordParams)
                });

            var restartSqlServerScript = this.settings.DeploymentScriptBlocks.RestartWindowsService;
            var restartSqlServerParams = new[] { this.settings.DatabaseServerSettings.SqlServiceName };
            databaseSteps.Add(
                new SetupStep
                {
                    Description = "Restart SQL server for account change(s) to take effect.",
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(restartSqlServerScript.ScriptText, restartSqlServerParams)
                });

            var connectionString = "Server=localhost; user id=sa; password=" + databaseStrategy.AdministratorPassword;
            var databaseFileNameSettings = (databaseStrategy.DatabaseSettings ?? new DatabaseSettings()).DatabaseFileNameSettings
                                            ?? new DatabaseFileNameSettings
                                                   {
                                                       DataFileLogicalName = databaseStrategy.Name + "Dat",
                                                       DataFileNameOnDisk = databaseStrategy.Name + ".mdb",
                                                       LogFileLogicalName = databaseStrategy.Name + "Log",
                                                       LogFileNameOnDisk = databaseStrategy.Name + ".log"
                                                   };
            var databaseFileSizeSettings = (databaseStrategy.DatabaseSettings ?? new DatabaseSettings()).DatabaseFileSizeSettings
                                            ?? this.settings.DefaultDatabaseFileSizeSettings;
            var databaseConfiguration = new DatabaseConfiguration
                                            {
                                                DatabaseName = databaseStrategy.Name,
                                                DatabaseType = DatabaseType.User,
                                                DataFileLogicalName = databaseFileNameSettings.DataFileLogicalName,
                                                DataFilePath = Path.Combine(dataDirectory, databaseFileNameSettings.DataFileNameOnDisk),
                                                DataFileCurrentSizeInKb = databaseFileSizeSettings.DataFileCurrentSizeInKb,
                                                DataFileMaxSizeInKb = databaseFileSizeSettings.DataFileMaxSizeInKb,
                                                DataFileGrowthSizeInKb = databaseFileSizeSettings.DataFileGrowthSizeInKb,
                                                LogFileLogicalName = databaseFileNameSettings.LogFileLogicalName,
                                                LogFilePath = Path.Combine(dataDirectory, databaseFileNameSettings.LogFileNameOnDisk),
                                                LogFileCurrentSizeInKb = databaseFileSizeSettings.LogFileCurrentSizeInKb,
                                                LogFileMaxSizeInKb = databaseFileSizeSettings.LogFileMaxSizeInKb,
                                                LogFileGrowthSizeInKb = databaseFileSizeSettings.LogFileGrowthSizeInKb
                                            };
            databaseSteps.Add(
                new SetupStep
                    {
                        Description = "Create database: " + databaseStrategy.Name,
                        SetupAction = machineManager =>
                            {
                                var realRemoteConnectionString = connectionString.Replace(
                                    "localhost",
                                    machineManager.IpAddress);
                                DatabaseManager.Create(realRemoteConnectionString, databaseConfiguration);
                            }
                    });

            if (databaseStrategy.Restore != null)
            {
                var awsRestore = databaseStrategy.Restore as DatabaseRestoreFromS3;
                if (awsRestore == null)
                {
                    throw new NotSupportedException(
                        "Currently no support for type of database restore: " + databaseStrategy.Restore.GetType());
                }

                databaseSteps.Add(
                    new SetupStep
                        {
                            Description =
                                string.Format(
                                    "Restore - Region: {0}; Bucket: {1}; File: {2}",
                                    awsRestore.Region,
                                    awsRestore.BucketName,
                                    awsRestore.FileName),
                            SetupAction = machineManager =>
                                {
                                    var restoreFilePath = Path.Combine(
                                        databaseStrategy.BackupDirectory,
                                        awsRestore.FileName);

                                    var remoteDownloadBackupScriptBlock =
                                        this.settings.DeploymentScriptBlocks.DownloadS3Object.ScriptText;
                                    var remoteDownloadBackupScriptParams = new[]
                                                                               {
                                                                                   awsRestore.BucketName,
                                                                                   awsRestore.FileName, restoreFilePath,
                                                                                   awsRestore.Region,
                                                                                   awsRestore.DownloadAccessKey,
                                                                                   awsRestore.DownloadSecretKey
                                                                               };

                                    machineManager.RunScript(
                                        remoteDownloadBackupScriptBlock,
                                        remoteDownloadBackupScriptParams);
                                    var realRemoteConnectionString = connectionString.Replace(
                                        "localhost",
                                        machineManager.IpAddress);

                                    var restoreFileUri = new Uri(restoreFilePath);
                                    var checksumOption = awsRestore.RunChecksum
                                                             ? ChecksumOption.Checksum
                                                             : ChecksumOption.NoChecksum;
                                    var restoreDetails = new RestoreDetails
                                                             {
                                                                 ChecksumOption = checksumOption,
                                                                 Device = Device.Disk,
                                                                 ErrorHandling = ErrorHandling.StopOnError,
                                                                 DataFilePath = databaseConfiguration.DataFilePath,
                                                                 LogFilePath = databaseConfiguration.LogFilePath,
                                                                 RecoveryOption = RecoveryOption.NoRecovery,
                                                                 ReplaceOption =
                                                                     ReplaceOption.ReplaceExistingDatabase,
                                                                 RestoreFrom = restoreFileUri,
                                                                 RestrictedUserOption =
                                                                     RestrictedUserOption.Normal
                                                             };
                                    DatabaseManager.RestoreFull(
                                        realRemoteConnectionString,
                                        databaseStrategy.Name,
                                        restoreDetails);
                                }
                        });
            }

            if (databaseStrategy.Migration != null)
            {
                var fluentMigration = databaseStrategy.Migration as DatabaseMigrationFluentMigrator;
                if (fluentMigration == null)
                {
                    throw new NotSupportedException(
                        "Currently no support for type of database migration: " + databaseStrategy.Migration.GetType());
                }

                databaseSteps.Add(
                    new SetupStep
                        {
                            Description = "Run Database Fluent Migration to Version: " + fluentMigration.Version,
                            SetupAction = machineManager =>
                                {
                                    var realRemoteConnectionString = connectionString.Replace(
                                        "localhost",
                                        machineManager.IpAddress);
                                    var migrationDllBytes =
                                        this.packageManager.GetMultipleFileContentsFromPackageAsBytes(
                                            package,
                                            package.PackageDescription.Id + ".dll").Select(_ => _.Value).SingleOrDefault();
                                    var migrationPdbBytes =
                                        this.packageManager.GetMultipleFileContentsFromPackageAsBytes(
                                            package,
                                            package.PackageDescription.Id + ".pdb").Select(_ => _.Value).SingleOrDefault();

                                    var assembly = migrationPdbBytes == null
                                                            ? Assembly.Load(migrationDllBytes)
                                                            : Assembly.Load(migrationDllBytes, migrationPdbBytes);

                                    MigrationExecutor.Up(
                                        assembly,
                                        realRemoteConnectionString,
                                        databaseStrategy.Name,
                                        fluentMigration.Version);
                                }
                        });
            }

            return databaseSteps;
        }

        private SetupStep GetCopyAndUnzipPackageStep(PackagedDeploymentConfiguration packagedConfig)
        {
            var packageDirectoryPath = GetPackageDirectoryPath(packagedConfig);
            var packageFilePath = Path.Combine(packageDirectoryPath, "Package.zip");
            var unzipScript = this.settings.DeploymentScriptBlocks.UnzipFile.ScriptText;
            var unzipParams = new[] { packageFilePath, packageDirectoryPath };
            var deployUnzippedFileStep = new SetupStep
                                             {
                                                 Description =
                                                     "Push package file and unzip: "
                                                     + packagedConfig.Package.PackageDescription
                                                           .GetIdDotVersionString(),
                                                 SetupAction = machineManager =>
                                                     {
                                                         machineManager.SendFile(
                                                             packageFilePath,
                                                             packagedConfig.Package.PackageFileBytes);
                                                         machineManager.RunScript(unzipScript, unzipParams);
                                                     }
                                             };

            return deployUnzippedFileStep;
        }

        private SetupStep GetChocolateySetupStep(PackagedDeploymentConfiguration packagedConfig)
        {
            SetupStep installChocoStep = null;
            if (packagedConfig.DeploymentConfiguration.ChocolateyPackages != null
                && packagedConfig.DeploymentConfiguration.ChocolateyPackages.Any())
            {
                installChocoStep = new SetupStep
                                       {
                                           Description =
                                               "Install Chocolatey Packages: "
                                               + string.Join(
                                                   ",",
                                                   packagedConfig.DeploymentConfiguration.ChocolateyPackages
                                                     .Select(_ => _.GetIdDotVersionString())),
                                           SetupAction = machineManager =>
                                               {
                                                   var scriptBlockParameters =
                                                       packagedConfig.DeploymentConfiguration
                                                           .ChocolateyPackages.Select(_ => _ as object)
                                                           .ToArray();

                                                   machineManager.RunScript(
                                                       this.settings.DeploymentScriptBlocks.InstallChocolatey
                                                           .ScriptText,
                                                       scriptBlockParameters);
                                               }
                                       };
            }

            return installChocoStep;
        }

        private static string GetPackageDirectoryPath(PackagedDeploymentConfiguration packagedConfig)
        {
            return Path.Combine(RootDeploymentPath, packagedConfig.Package.PackageDescription.Id);
        }
    }
}
