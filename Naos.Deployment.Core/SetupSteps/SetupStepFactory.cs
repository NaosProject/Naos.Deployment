// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;
    using Naos.Logging.Persistence;
    using Naos.Packaging.Domain;

    using OBeautifulCode.Validation.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
    internal partial class SetupStepFactory
    {
        private readonly IGetCertificates certificateRetriever;

        private readonly IGetPackages packageManager;

        private readonly IManageConfigFiles configFileManager;

        private readonly string environmentCertificateName;

        private readonly string workingDirectory;

        private readonly Action<string> debugAnnouncer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetupStepFactory"/> class.
        /// </summary>
        /// <param name="settings">Settings for the factory.</param>
        /// <param name="certificateRetriever">Certificate retriever to get certificates for steps.</param>
        /// <param name="packageManager">Package manager to use for getting package files contents.</param>
        /// <param name="configFileManager">Config file manager to use when creating or manipulating config files.</param>
        /// <param name="environmentCertificateName">Optional name of the environment certificate to be found in the CertificateManager provided.</param>
        /// <param name="workingDirectory">Working directory to create scratch files.</param>
        /// <param name="debugAnnouncer">Announcer for events.</param>
        public SetupStepFactory(SetupStepFactorySettings settings, IGetCertificates certificateRetriever, IGetPackages packageManager, IManageConfigFiles configFileManager, string environmentCertificateName, string workingDirectory, Action<string> debugAnnouncer)
        {
            this.Settings = settings;
            this.packageManager = packageManager;
            this.certificateRetriever = certificateRetriever;
            this.configFileManager = configFileManager;
            this.environmentCertificateName = environmentCertificateName;
            this.workingDirectory = workingDirectory;
            this.debugAnnouncer = debugAnnouncer;
        }

        /// <summary>
        /// Gets the administrator account name.
        /// </summary>
        public string AdministratorAccount => this.Settings.AdministratorAccount;

        /// <summary>
        /// Gets the initialization strategy types that require the package bytes to be copied up to the target server.
        /// </summary>
        public IReadOnlyCollection<Type> InitializationStrategyTypesThatNeedPackageBytes => this.Settings.InitializationStrategyTypesThatNeedPackageBytes;

        /// <summary>
        /// Gets the initialization strategy types that require the package bytes to be copied up to the target server.
        /// </summary>
        public IReadOnlyCollection<Type> InitializationStrategyTypesThatNeedEnvironmentCertificate => this.Settings.InitializationStrategyTypesThatNeedEnvironmentCertificate;

        /// <summary>
        /// Gets the max number of times to execute a setup step before throwing.
        /// </summary>
        public int MaxSetupStepAttempts => this.Settings.MaxSetupStepAttempts;

        /// <summary>
        /// Gets a value indicating whether or not to throw if the max attempts are not successful on a setup step.
        /// </summary>
        public bool ThrowOnFailedSetupStep => this.Settings.ThrowOnFailedSetupStep;

        /// <summary>
        /// Gets the list of directories we've found people add to packages and contain assemblies that fail to load correctly in reflection and are not be necessary for normal function.
        /// </summary>
        public IReadOnlyCollection<string> RootPackageDirectoriesToPrune => this.Settings.RootPackageDirectoriesToPrune;

        /// <summary>
        /// Gets the underlying settings used to create the factory.
        /// </summary>
        public SetupStepFactorySettings Settings { get; private set; }

        /// <summary>
        /// Get the appropriate setup steps for the packaged config.
        /// </summary>
        /// <param name="packagedConfig">Config to base setup steps from.</param>
        /// <param name="environment">Environment that is being deployed.</param>
        /// <param name="adminPassword">Administrator password for the machine in case an application needs to be run as that user (which is discouraged!).</param>
        /// <param name="funcToCreateNewDnsWithTokensReplaced">Function to apply any token replacements to a DNS entry.</param>
        /// <returns>Collection of setup steps that will leave the machine properly configured.</returns>
        public async Task<IReadOnlyCollection<SetupStepBatch>> GetSetupStepsAsync(PackagedDeploymentConfiguration packagedConfig, string environment, string adminPassword, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            ThrowIfMultipleMongoStrategiesAreInvalidCombination(packagedConfig.GetInitializationStrategiesOf<InitializationStrategyMongo>());

            ThrowIfCertificatesNotProperlyConfigured(packagedConfig.GetInitializationStrategiesOf<InitializationStrategyCertificateToInstall>(), packagedConfig.GetInitializationStrategiesOf<InitializationStrategySelfHost>(), packagedConfig.GetInitializationStrategiesOf<InitializationStrategyIis>());

            ThrowIfMissingNecessaryVolumes(packagedConfig.DeploymentConfiguration.Volumes, packagedConfig.GetInitializationStrategiesOf<InitializationStrategySqlServer>(), packagedConfig.GetInitializationStrategiesOf<InitializationStrategyMongo>());

            var ret = new List<SetupStepBatch>();

            var distinctInitializationStrategyTypes = packagedConfig.InitializationStrategies.Select(_ => _.GetType()).Distinct().ToList();

            // only copy the package byes if there are initialization strategies on the package that require it...
            if (distinctInitializationStrategyTypes.Any(_ => this.InitializationStrategyTypesThatNeedPackageBytes.Contains(_)))
            {
                var deployUnzippedFileStep = this.GetCopyAndUnzipPackageStep(packagedConfig);
                var batch = new SetupStepBatch
                                {
                                    ExecutionOrder = ExecutionOrder.CopyPackages,
                                    Steps = new[] { deployUnzippedFileStep },
                                };
                ret.Add(batch);
            }

            if (distinctInitializationStrategyTypes.Any(_ => _ == typeof(InitializationStrategyMongo)))
            {
                var installMongoSteps = this.GetInstallMongoSteps();
                var batch = new SetupStepBatch { ExecutionOrder = ExecutionOrder.InstallMongo, Steps = installMongoSteps, };
                ret.Add(batch);
            }

            foreach (var initializationStrategy in packagedConfig.InitializationStrategies)
            {
                var initSteps = await this.GetStrategySpecificSetupStepBatchesAsync(initializationStrategy, packagedConfig, environment, adminPassword, funcToCreateNewDnsWithTokensReplaced);
                ret.AddRange(initSteps);
            }

            return ret;
        }

        private async Task<IReadOnlyCollection<SetupStepBatch>> GetStrategySpecificSetupStepBatchesAsync(InitializationStrategyBase strategy, PackagedDeploymentConfiguration packagedConfig, string environment, string adminPassword, Func<string, string> funcToReplaceKnownTokensWithValues)
        {
            IReadOnlyCollection<SetupStepBatch> ret = new SetupStepBatch[0];
            var package = packagedConfig.PackageWithBundleIdentifier.Package;
            var packageId = package.PackageDescription.Id;
            var packageDirectoryPath = this.GetPackageDirectoryPath(packagedConfig);
            var defaultLogWritingSettings = this.GetDefaultLogWritingSettings(packagedConfig);

            if (strategy is InitializationStrategyReplaceTokenInFiles replaceTokenStrategy)
            {
                var tokenSteps = this.GetReplaceTokenInFilesSpecificSteps(replaceTokenStrategy, packageId, packageDirectoryPath, funcToReplaceKnownTokensWithValues);

                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.ReplaceTokenInFiles,
                                      Steps = tokenSteps,
                                  },
                          };
            }
            else if (strategy is InitializationStrategyDirectoryToCreate directoryToCreateStrategy)
            {
                var dirSteps = this.GetDirectoryToCreateSpecificSteps(
                    directoryToCreateStrategy,
                    packageId,
                    funcToReplaceKnownTokensWithValues);

                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.CreateDirectory,
                                      Steps = dirSteps,
                                  },
                          };
            }
            else if (strategy is InitializationStrategyCreateEventLog eventLogToCreateStrategy)
            {
                var eventLogSteps = this.GetCreateEventLogSpecificSteps(eventLogToCreateStrategy, packageId);
                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.CreateEventLog,
                                      Steps = eventLogSteps,
                                  },
                          };
            }
            else if (strategy is InitializationStrategyCertificateToInstall certToInstallStrategy)
            {
                var certSteps =
                    await
                        this.GetCertificateToInstallSpecificStepsAsync(
                            certToInstallStrategy,
                            packageId,
                            packageDirectoryPath,
                            funcToReplaceKnownTokensWithValues);

                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.InstallCertificate,
                                      Steps = certSteps,
                                  },
                          };
            }
            else if (strategy is InitializationStrategySqlServer sqlServerStrategy)
            {
                var databaseSteps = this.GetSqlServerSpecificSteps(sqlServerStrategy, package);

                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.SqlServer,
                                      Steps = databaseSteps,
                                  },
                          };
            }
            else if (strategy is InitializationStrategyMongo mongoStrategy)
            {
                var mongoSteps = this.GetConfigureMongoSteps(mongoStrategy);

                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.ConfigureMongo,
                                      Steps = mongoSteps,
                                  },
                          };
            }
            else if (strategy is InitializationStrategyMessageBusHandler)
            {
                /* No additional steps necessary as the work would be done in a deployment adjuster if needed */
            }
            else if (strategy is InitializationStrategyDnsEntry)
            {
                /* No additional steps necessary as the DeploymentManager performs this operation directly */
            }
            else if (strategy is InitializationStrategyCopyBytes)
            {
                /* No additional steps necessary as the files will be copied by inclusion in the copy white list */
            }
            else if (strategy is InitializationStrategyScheduledTask scheduledTaskStrategy)
            {
                var scheduledTaskSteps =
                    this.GetScheduledTaskSpecificSteps(
                        scheduledTaskStrategy,
                        defaultLogWritingSettings,
                        packagedConfig.ItsConfigOverrides,
                        packageDirectoryPath,
                        environment,
                        adminPassword);

                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.ScheduledTask,
                                      Steps = scheduledTaskSteps,
                                  },
                          };
            }
            else if (strategy is InitializationStrategyOnetimeCall onetimeCallStrategy)
            {
                var onetimeCallSteps = this.GetOnetimeCallSpecificSteps(
                    onetimeCallStrategy,
                    defaultLogWritingSettings,
                    packagedConfig.ItsConfigOverrides,
                    packageDirectoryPath,
                    environment);

                var oneTimeExecutionOrder = onetimeCallStrategy.SetupStepExecutionSlot.GetOneTimeExecutionOrder();
                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = oneTimeExecutionOrder,
                                      Steps = onetimeCallSteps,
                                  },
                          };
            }
            else if (strategy is InitializationStrategySelfHost selfHostStrategy)
            {
                var selfHostSteps =
                    await
                    this.GetSelfHostSpecificSteps(
                        selfHostStrategy,
                        defaultLogWritingSettings,
                        packagedConfig.ItsConfigOverrides,
                        packageDirectoryPath,
                        environment,
                        adminPassword,
                        funcToReplaceKnownTokensWithValues);

                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.SelfHost,
                                      Steps = selfHostSteps,
                                  },
                          };
            }
            else if (strategy is InitializationStrategyIis iisStrategy)
            {
                var webRootPath = Path.Combine(packageDirectoryPath, "packagedWebsite"); // this needs to match how the package was built in the build system...

                var webStepsAfterReboot = await this.GetIisSpecificSetupStepsAsync(
                                   iisStrategy,
                                   defaultLogWritingSettings,
                                   packagedConfig.ItsConfigOverrides,
                                   webRootPath,
                                   environment,
                                   adminPassword,
                                   funcToReplaceKnownTokensWithValues);

                ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.ConfigureIis,
                                      Steps = webStepsAfterReboot,
                                  },
                          };
            }
            else
            {
                throw new DeploymentException("The initialization strategy type is not supported: " + strategy.GetType());
            }

            return ret;
        }

        private string GetAccountToUse(InitializationStrategyBase strategy)
        {
            if (strategy.GetType() == typeof(InitializationStrategyIis))
            {
                return this.GetAccountToUse((InitializationStrategyIis)strategy);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyScheduledTask))
            {
                return this.GetAccountToUse((InitializationStrategyScheduledTask)strategy);
            }
            else if (strategy.GetType() == typeof(InitializationStrategySelfHost))
            {
                return this.GetAccountToUse((InitializationStrategySelfHost)strategy);
            }
            else
            {
                return null;
            }
        }

        private SetupStep GetCopyAndUnzipPackageStep(PackagedDeploymentConfiguration packagedConfig)
        {
            var packageDirectoryPath = this.GetPackageDirectoryPath(packagedConfig);
            var packageFilePath = Path.Combine(packageDirectoryPath, "Package.zip");
            var unzipScript = this.Settings.DeploymentScriptBlocks.UnzipFile.ScriptText;
            var unzipParams = new[] { packageFilePath, packageDirectoryPath };
            var deployUnzippedFileStep = new SetupStep
                                             {
                                                 Description = Invariant($"Push package file and unzip: {packagedConfig.PackageWithBundleIdentifier.Package.PackageDescription.GetIdDotVersionString()}."),
                                                 SetupFunc = machineManager =>
                                                     {
                                                         // in case we're in a retry scenario we should just overwrite...
                                                         const bool Overwrite = true;
                                                         machineManager.SendFile(
                                                             packageFilePath,
                                                             packagedConfig.PackageWithBundleIdentifier.Package.PackageFileBytes,
                                                             false,
                                                             Overwrite);
                                                         return machineManager.RunScript(unzipScript, unzipParams).ToList();
                                                     },
                                             };

            return deployUnzippedFileStep;
        }

        private string GetPackageDirectoryPath(PackagedDeploymentConfiguration packagedConfig)
        {
            var volumes = packagedConfig.DeploymentConfiguration.Volumes;
            var rootDeploymentPath = this.Settings.BuildRootDeploymentPath(volumes);
            return Path.Combine(rootDeploymentPath, packagedConfig.PackageWithBundleIdentifier.Package.PackageDescription.Id);
        }

        private LogWritingSettings GetDefaultLogWritingSettings(PackagedDeploymentConfiguration packagedConfig)
        {
            var volumes = packagedConfig.DeploymentConfiguration.Volumes;
            var deploymentDriveLetter = this.Settings.GetDeploymentDriveLetter(volumes);
            var packageName = packagedConfig.PackageWithBundleIdentifier.Package.PackageDescription.Id;
            return this.Settings.BuildDefaultLogWritingSettings(deploymentDriveLetter, packageName);
        }

        private static void ThrowIfMissingNecessaryVolumes(IReadOnlyCollection<Volume> volumes, IReadOnlyCollection<InitializationStrategySqlServer> initializationStrategiesSql, IReadOnlyCollection<InitializationStrategyMongo> initializationStrategiesMongo)
        {
            if ((initializationStrategiesSql?.Any() ?? false) || (initializationStrategiesMongo?.Any() ?? false))
            {
                if (!(volumes?.Any(_ => "D".Equals((_?.DriveLetter ?? string.Empty).ToUpperInvariant())) ?? false))
                {
                    throw new DeploymentException(Invariant($"Must have a 'D' volume to use initialization strategies: {nameof(InitializationStrategySqlServer)}, {nameof(InitializationStrategyMongo)}"));
                }
            }
        }

        private static void ThrowIfCertificatesNotProperlyConfigured(
            IReadOnlyCollection<InitializationStrategyCertificateToInstall> certificateToInstallStrategies,
            IReadOnlyCollection<InitializationStrategySelfHost> selfHostStrategies,
            IReadOnlyCollection<InitializationStrategyIis> iisStrategies)
        {
            void VerifyCertificate(string certName, string description)
            {
                var certToInstall = certificateToInstallStrategies.SingleOrDefault(_ => _.CertificateToInstall == certName);
                if (certToInstall == null)
                {
                    throw new DeploymentException(Invariant($"Failed to find an initialization strategy to install the required certificate: {certName}.  {description}"));
                }

                if ((certToInstall.StoreLocation ?? StoreLocation.LocalMachine) != StoreLocation.LocalMachine)
                {
                    throw new DeploymentException(Invariant($"Certificate Store Location must be {StoreLocation.LocalMachine} instead of {certToInstall.StoreLocation} for cert: {certName}.  {description}"));
                }

                if ((certToInstall.StoreName ?? StoreName.My) != StoreName.My)
                {
                    throw new DeploymentException(Invariant($"Certificate Store Name must be {StoreName.My} instead of {certToInstall.StoreName} for cert: {certName}.  {description}"));
                }
            }

            foreach (var selfHost in selfHostStrategies)
            {
                VerifyCertificate(selfHost.SslCertificateName, Invariant($"Self Host Exe: {selfHost.SelfHostExeFilePathRelativeToPackageRoot}"));
            }

            foreach (var iis in iisStrategies)
            {
                foreach (var httpsBinding in iis.HttpsBindings ?? new HttpsBinding[0])
                {
                    VerifyCertificate(httpsBinding.SslCertificateName, Invariant($"IIS: {iis.PrimaryDns}"));
                }
            }
        }
    }

    /// <summary>
    /// Extensions to <see cref="SetupStepFactorySettings" />.
    /// </summary>
    public static class SetupFactorySettingsExtensions
    {
        /// <summary>
        /// Builds a root deployment path from the provided settings with any additional context that is required.
        /// </summary>
        /// <param name="settings">Settings to use.</param>
        /// <param name="volumes">Volumes of current instance.</param>
        /// <returns>Root deployment path to use.</returns>
        public static string BuildRootDeploymentPath(this SetupStepFactorySettings settings, IReadOnlyCollection<Volume> volumes)
        {
            new { settings }.Must().NotBeNull();

            var deploymentDriveLetter = settings.GetDeploymentDriveLetter(volumes);

            var ret = TokenSubstitutions.GetSubstitutedStringForPath(settings.RootDeploymentPathTemplate, deploymentDriveLetter);
            return ret;
        }

        /// <summary>
        /// Gets the deployment drive letter from the provided settings with any additional context that is required.
        /// </summary>
        /// <param name="settings">Settings to use.</param>
        /// <param name="volumes">Volumes of current instance.</param>
        /// <returns>Deployment drive letter to use.</returns>
        public static string GetDeploymentDriveLetter(this SetupStepFactorySettings settings, IReadOnlyCollection<Volume> volumes)
        {
            new { settings }.Must().NotBeNull();
            new { volumes }.Must().NotBeNullNorEmptyEnumerableNorContainAnyNulls();

            var driveLetters = volumes.Select(_ => _.DriveLetter).ToList();
            string deploymentDriveLetter = null;
            foreach (var driveLetterToCheck in settings.DeploymentDriveLetterPrecedence)
            {
                deploymentDriveLetter = driveLetters.FirstOrDefault(_ => _ == driveLetterToCheck);
                if (!string.IsNullOrWhiteSpace(deploymentDriveLetter))
                {
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(deploymentDriveLetter))
            {
                throw new ArgumentException(
                    Invariant(
                        $"Must specify a drive in the {nameof(settings.DeploymentDriveLetterPrecedence)}; expected one of ({string.Join(",", settings.DeploymentDriveLetterPrecedence)}); found ({string.Join(",", driveLetters)})."));
            }

            return deploymentDriveLetter;
        }

        /// <summary>
        /// Build a default <see cref="LogWritingSettings" /> to add to deployments.
        /// </summary>
        /// <param name="settings">Settings to use.</param>
        /// <param name="deploymentDriveLetter">Path to use.</param>
        /// <param name="packageName">Name to use.</param>
        /// <returns>Configured <see cref="LogWritingSettings" />.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "logfile", Justification = "Spelling/name is correct.")]
        public static LogWritingSettings BuildDefaultLogWritingSettings(this SetupStepFactorySettings settings, string deploymentDriveLetter, string packageName)
        {
            new { settings }.Must().NotBeNull();
            new { deploymentDriveLetter }.Must().NotBeNullNorWhiteSpace();
            new { packageName }.Must().NotBeNullNorWhiteSpace();

            var template = settings.DefaultLogWritingSettings;
            var updatedConfigurations = template.Configs.Select(_ => UpdateFilePathInfoOnLoggingConfigurations(_, deploymentDriveLetter, packageName)).ToList();
            var ret = new LogWritingSettings(updatedConfigurations);
            return ret;
        }

        private static LogWriterConfigBase UpdateFilePathInfoOnLoggingConfigurations(LogWriterConfigBase logConfiguration, string deploymentDriveLetter, string packageName)
        {
            LogWriterConfigBase ret;
            if (logConfiguration is FileLogConfig file)
            {
                var path = Path.GetDirectoryName(file.LogFilePath) ?? string.Empty;
                var detokenedPath = TokenSubstitutions.GetSubstitutedStringForPath(path, deploymentDriveLetter);
                var fileName = Path.GetFileName(file.LogFilePath) ?? string.Empty;
                var updatedLogPath = Path.Combine(detokenedPath, packageName + "-" + fileName);
                ret = new FileLogConfig(file.LogInclusionKindToOriginsMap, updatedLogPath, file.CreateDirectoryStructureIfMissing, file.LogItemPropertiesToIncludeInLogMessage);
            }
            else if (logConfiguration is TimeSlicedFilesLogConfig sliced)
            {
                var detokenedPath = TokenSubstitutions.GetSubstitutedStringForPath(sliced.LogFileDirectoryPath, deploymentDriveLetter);
                var updatedLogDirectoryPath = Path.Combine(detokenedPath, packageName);
                var updatedFileNamePrefix = packageName + "-" + sliced.FileNamePrefix;
                ret = new TimeSlicedFilesLogConfig(sliced.LogInclusionKindToOriginsMap, updatedLogDirectoryPath, updatedFileNamePrefix, sliced.TimeSlicePerFile, sliced.CreateDirectoryStructureIfMissing, sliced.LogItemPropertiesToIncludeInLogMessage);
            }
            else
            {
                throw new NotSupportedException(Invariant($"Unsupported {nameof(LogWriterConfigBase)} in {nameof(SetupStepFactorySettings)}.{nameof(SetupStepFactorySettings.DefaultLogWritingSettings)}; {logConfiguration.GetType()}"));
            }

            return ret;
        }
    }
}
