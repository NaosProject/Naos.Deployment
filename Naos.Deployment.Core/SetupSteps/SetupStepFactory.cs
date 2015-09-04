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

    using Its.Log.Instrumentation;

    using Naos.Database.Contract;
    using Naos.Database.Migrator;
    using Naos.Database.Tools;
    using Naos.Deployment.Contract;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private readonly IGetCertificates certificateRetriever;

        private readonly SetupStepFactorySettings settings;

        private readonly IManagePackages packageManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetupStepFactory"/> class.
        /// </summary>
        /// <param name="settings">Settings for the factory.</param>
        /// <param name="certificateRetriever">Certificate retriever to get certificates for steps.</param>
        /// <param name="packageManager">Package manager to use for getting package files contents.</param>
        public SetupStepFactory(SetupStepFactorySettings settings, IGetCertificates certificateRetriever, IManagePackages packageManager)
        {
            this.certificateRetriever = certificateRetriever;
            this.settings = settings;
            this.packageManager = packageManager;
        }

        /// <summary>
        /// Gets the root deployment path.
        /// </summary>
        public string RootDeploymentPath
        {
            get
            {
                return this.settings.RootDeploymentPath;
            }
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
            if (packagedConfig.GetInitializationStrategiesOf<InitializationStrategySqlServer>().Count()
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
            var packageDirectoryPath = this.GetPackageDirectoryPath(packagedConfig);

            if (strategy.GetType() == typeof(InitializationStrategyIis))
            {
                var webRootPath = Path.Combine(packageDirectoryPath, "packagedWebsite"); // this needs to match how the package was built in the build system...
                var webSteps = this.GetWebSpecificSetupSteps(
                    (InitializationStrategyIis)strategy,
                    packagedConfig.ItsConfigOverrides,
                    packageDirectoryPath,
                    webRootPath,
                    environment);
                ret.AddRange(webSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategySqlServer))
            {
                var databaseSteps = this.GetSqlServerSpecificSteps((InitializationStrategySqlServer)strategy, packagedConfig.Package);
                ret.AddRange(databaseSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyMessageBusHandler))
            {
                /* No additional steps necessary as the DeploymentManager should have included a harness by virtue of this type of initialization strategy */
            }
            else if (strategy.GetType() == typeof(InitializationStrategyPrivateDnsEntry))
            {
                /* No additional steps necessary as the DeploymentManager performs this operation at the end */
            }
            else if (strategy.GetType() == typeof(InitializationStrategyDirectoryToCreate))
            {
                var dirSteps = this.GetDirectoryToCreateSpecificSteps(
                    (InitializationStrategyDirectoryToCreate)strategy,
                    this.settings.HarnessSettings.HarnessAccount);
                ret.AddRange(dirSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyCertificateToInstall))
            {
                var certSteps =
                    this.GetCertificateToInstallSpecificSteps(
                        (InitializationStrategyCertificateToInstall)strategy,
                        packageDirectoryPath);
                ret.AddRange(certSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyMongo))
            {
                var mongoSteps =
                    this.GetMongoSpecificSteps(
                        (InitializationStrategyMongo)strategy);
                ret.AddRange(mongoSteps);
            }
            else
            {
                throw new DeploymentException("The initialization strategy type is not supported: " + strategy.GetType());
            }

            return ret;
        }

        private List<SetupStep> GetCertificateToInstallSpecificSteps(InitializationStrategyCertificateToInstall certToInstallStrategy, string packageDirectoryPath)
        {
            var certSteps = new List<SetupStep>();

            var certificateName = certToInstallStrategy.CertificateToInstall;

            var certDetails = this.certificateRetriever.GetCertificateByName(certificateName);
            if (certDetails == null)
            {
                throw new DeploymentException("Could not find certificate by name: " + certificateName);
            }

            var certificateTargetPath = Path.Combine(packageDirectoryPath, certDetails.GenerateFileName());
            certSteps.Add(
                new SetupStep
                    {
                        Description =
                            "Send certificate file (removed after installation): "
                            + certDetails.GenerateFileName(),
                        SetupAction =
                            machineManager =>
                            machineManager.SendFile(certificateTargetPath, certDetails.FileBytes)
                    });

            var installCertificateParams = new object[] { certificateTargetPath, certDetails.CertificatePassword };

            certSteps.Add(
                new SetupStep
                    {
                        Description = "Installing certificate: " + certificateName,
                        SetupAction =
                            machineManager =>
                            machineManager.RunScript(
                                this.settings.DeploymentScriptBlocks.InstallCertificate.ScriptText,
                                installCertificateParams)
                    });

            return certSteps;
        }

        private List<SetupStep> GetDirectoryToCreateSpecificSteps(InitializationStrategyDirectoryToCreate directoryToCreateStrategy, string harnessAccount)
        {
            var dir = directoryToCreateStrategy.DirectoryToCreate;
            var fullControlAccount = dir.FullControlAccount.Replace("{harnessAccount}", harnessAccount);
            var dirParams = new object[] { dir.FullPath, fullControlAccount };
            var ret = new SetupStep
                          {
                              Description =
                                  "Creating directory: " + dir.FullPath + " with full control granted to: "
                                  + fullControlAccount,
                              SetupAction =
                                  machineManager =>
                                  machineManager.RunScript(
                                      this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl
                                      .ScriptText,
                                      dirParams)
                          };

            return new[] { ret }.ToList();
        }

        private List<SetupStep> GetWebSpecificSetupSteps(InitializationStrategyIis iisStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string packageDirectoryPath, string webRootPath, string environment)
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
                var itsFileBytes = Encoding.UTF8.GetBytes(itsConfigOverride.FileContentsJson);

                webSteps.Add(
                    new SetupStep 
                        {
                            Description =
                                "(Over)write Its.Config file: " + itsConfigOverride.FileNameWithoutExtension,
                            SetupAction =
                                machineManager => machineManager.SendFile(itsFilePath, itsFileBytes, false, true)
                        });
            }

            var certDetails = this.certificateRetriever.GetCertificateByName(iisStrategy.SslCertificateName);
            if (certDetails == null)
            {
                throw new DeploymentException("Could not find certificate by name: " + iisStrategy.SslCertificateName);
            }

            var certificateTargetPath = Path.Combine(packageDirectoryPath, certDetails.GenerateFileName());
            var appPoolStartMode = iisStrategy.AppPoolStartMode == ApplicationPoolStartMode.None
                                       ? ApplicationPoolStartMode.OnDemand
                                       : iisStrategy.AppPoolStartMode;

            var autoStartProviderName = iisStrategy.AutoStartProvider == null
                                            ? null
                                            : iisStrategy.AutoStartProvider.Name;
            var autoStartProviderType = iisStrategy.AutoStartProvider == null
                                            ? null
                                            : iisStrategy.AutoStartProvider.Type;

            const bool EnableSni = false;
            const bool AddHostHeaders = true;
            var installWebParameters = new object[]
                                           {
                                               webRootPath, iisStrategy.PrimaryDns, certificateTargetPath,
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

        private DatabaseConfiguration BuildDatabaseConfiguration(
            string databaseName,
            string dataDirectory,
            DatabaseFileNameSettings databaseFileNameSettings,
            DatabaseFileSizeSettings databaseFileSizeSettings)
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

            var databaseConfiguration = new DatabaseConfiguration
                                            {
                                                DatabaseName = databaseName,
                                                DatabaseType = DatabaseType.User,
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

        private SetupStep GetCopyAndUnzipPackageStep(PackagedDeploymentConfiguration packagedConfig)
        {
            var packageDirectoryPath = this.GetPackageDirectoryPath(packagedConfig);
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
                                                         // don't push the null package...
                                                         if (!string.Equals(
                                                                 packagedConfig.Package.PackageDescription.Id,
                                                                 PackageManager.NullPackageId))
                                                         {
                                                             machineManager.SendFile(
                                                                 packageFilePath,
                                                                 packagedConfig.Package.PackageFileBytes);
                                                             Log.Write(
                                                                 () =>
                                                                 machineManager.RunScript(unzipScript, unzipParams));
                                                         }
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

                                                   var output = machineManager.RunScript(
                                                       this.settings.DeploymentScriptBlocks.InstallChocolatey
                                                           .ScriptText,
                                                       scriptBlockParameters);

                                                   // Log.Write(() => string.Join(Environment.NewLine + "   ", output));
                                               }
                                       };
            }

            return installChocoStep;
        }

        private string GetPackageDirectoryPath(PackagedDeploymentConfiguration packagedConfig)
        {
            return Path.Combine(this.RootDeploymentPath, packagedConfig.Package.PackageDescription.Id);
        }
    }
}
