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
    using System.Text;

    using Naos.Database.Tools;
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

        public SetupStepFactory(SetupStepFactorySettings settings, IGetCertificates certificateManager)
        {
            this.certificateManager = certificateManager;
            this.settings = settings;
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

            var installChocoStep = GetChocolateySetupStep(packagedConfig);
            if (installChocoStep != null)
            {
                ret.Add(installChocoStep);
            }

            var deployUnzippedFileStep = this.GetCopyAndUnzipPackageStep(packagedConfig);
            ret.Add(deployUnzippedFileStep);

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
                var databaseSteps = this.GetDatabaseSpecificSteps((InitializationStrategyDatabase)strategy);
                ret.AddRange(databaseSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyMessageBusHandler))
            {
                /* No additional steps necessary as the DeploymentManager should have included a harness by virtue of this type of initialization strategy */
            }
            else
            {
                throw new NotSupportedException("The initialization strategy type is not supported: " + strategy.GetType());
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

        private List<SetupStep> GetDatabaseSpecificSteps(InitializationStrategyDatabase databaseStrategy)
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
                    Description = "Enable SA account and set password.",
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(enableSaSetPasswordScript.ScriptText, enableSaSetPasswordParams)
                });

            var connectionString = "Server=localhost; user id=sa; password=" + databaseStrategy.AdministratorPassword;
            var databaseFileNameSettings = databaseStrategy.DatabaseSettings.DatabaseFileNameSettings
                                            ?? new DatabaseFileNameSettings
                                                   {
                                                       DataFileLogicalName = databaseStrategy.DatabaseName + "Dat",
                                                       DataFileNameOnDisk = databaseStrategy.DatabaseName + ".mdb",
                                                       LogFileLogicalName = databaseStrategy.DatabaseName + "Log",
                                                       LogFileNameOnDisk = databaseStrategy.DatabaseName + ".log"
                                                   };
            var databaseFileSizeSettings = databaseStrategy.DatabaseSettings.DatabaseFileSizeSettings
                                            ?? this.settings.DefaultDatabaseFileSizeSettings;
            var databaseConfiguration = new DatabaseConfiguration
                                            {
                                                DatabaseName = databaseStrategy.DatabaseName,
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
                    Description = "Create database: " + databaseStrategy.DatabaseName,
                    SetupAction =
                        machineManager =>
                        DatabaseManager.Create(connectionString, databaseConfiguration)
                });

            // TODO: finish out these optional scenarios...
            // Create/add necessary users (and roles)?
            // Restore from backup?
            // Apply Migration?
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

        private static SetupStep GetChocolateySetupStep(PackagedDeploymentConfiguration packagedConfig)
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
                        var installChocolateyPackagesLines =
                            packagedConfig.DeploymentConfiguration.ChocolateyPackages
                                .Select(
                                    _ =>
                                    _.Version == null
                                        ? "choco install " + _.Id + " -y"
                                        : "choco install " + _.Id + " -Version "
                                          + _.Version + " -y");
                        var installChocolateyPackagesScriptBlock = "{"
                                                                   + string.Join(
                                                                       Environment
                                                                         .NewLine,
                                                                       installChocolateyPackagesLines)
                                                                   + "}";
                        machineManager.RunScript(
                            installChocolateyPackagesScriptBlock,
                            new object[0]);
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
