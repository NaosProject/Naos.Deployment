// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
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

    using Its.Log.Instrumentation;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
    internal partial class SetupStepFactory
    {
        private readonly IGetCertificates certificateRetriever;

        private readonly SetupStepFactorySettings settings;

        private readonly IGetPackages packageManager;

        private readonly string[] itsConfigPrecedenceAfterEnvironment;

        private readonly string environmentCertificateName;

        private readonly string workingDirectory;

        private readonly Action<string> debugAnnouncer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetupStepFactory"/> class.
        /// </summary>
        /// <param name="settings">Settings for the factory.</param>
        /// <param name="certificateRetriever">Certificate retriever to get certificates for steps.</param>
        /// <param name="packageManager">Package manager to use for getting package files contents.</param>
        /// <param name="itsConfigPrecedenceAfterEnvironment">Its.Config precedence chain to be applied after the environment during any setup steps concerned with it.</param>
        /// <param name="environmentCertificateName">Optional name of the environment certificate to be found in the CertificateManager provided.</param>
        /// <param name="workingDirectory">Working directory to create scratch files.</param>
        /// <param name="debugAnnouncer">Announcer for events.</param>
        public SetupStepFactory(SetupStepFactorySettings settings, IGetCertificates certificateRetriever, IGetPackages packageManager, string[] itsConfigPrecedenceAfterEnvironment, string environmentCertificateName, string workingDirectory, Action<string> debugAnnouncer)
        {
            this.certificateRetriever = certificateRetriever;
            this.settings = settings;
            this.packageManager = packageManager;
            this.itsConfigPrecedenceAfterEnvironment = itsConfigPrecedenceAfterEnvironment;
            this.environmentCertificateName = environmentCertificateName;
            this.workingDirectory = workingDirectory;
            this.debugAnnouncer = debugAnnouncer;
        }

        /// <summary>
        /// Gets the administrator account name.
        /// </summary>
        public string AdministratorAccount => this.settings.AdministratorAccount;

        /// <summary>
        /// Gets the root deployment path.
        /// </summary>
        public string RootDeploymentPath => this.settings.RootDeploymentPath;

        /// <summary>
        /// Gets the initialization strategy types that require the package bytes to be copied up to the target server.
        /// </summary>
        public IReadOnlyCollection<Type> InitializationStrategyTypesThatNeedPackageBytes => this.settings.InitializationStrategyTypesThatNeedPackageBytes;

        /// <summary>
        /// Gets the initialization strategy types that require the package bytes to be copied up to the target server.
        /// </summary>
        public IReadOnlyCollection<Type> InitializationStrategyTypesThatNeedEnvironmentCertificate => this.settings.InitializationStrategyTypesThatNeedEnvironmentCertificate;

        /// <summary>
        /// Gets the max number of times to execute a setup step before throwing.
        /// </summary>
        public int MaxSetupStepAttempts => this.settings.MaxSetupStepAttempts;

        /// <summary>
        /// Gets a value indicating whether or not to throw if the max attempts are not successful on a setup step.
        /// </summary>
        public bool ThrowOnFailedSetupStep => this.settings.ThrowOnFailedSetupStep;

        /// <summary>
        /// Gets the list of directories we've found people add to packages and contain assemblies that fail to load correctly in reflection and are not be necessary for normal function.
        /// </summary>
        public IReadOnlyCollection<string> RootPackageDirectoriesToPrune => this.settings.RootPackageDirectoriesToPrune;

        /// <summary>
        /// Get the appropriate setup steps for the packaged config.
        /// </summary>
        /// <param name="packagedConfig">Config to base setup steps from.</param>
        /// <param name="environment">Environment that is being deployed.</param>
        /// <param name="adminPassword">Administrator password for the machine in case an application needs to be run as that user (which is discouraged!).</param>
        /// <param name="funcToCreateNewDnsWithTokensReplaced">Function to apply any token replacements to a DNS entry.</param>
        /// <returns>Collection of setup steps that will leave the machine properly configured.</returns>
        public async Task<ICollection<SetupStepBatch>> GetSetupStepsAsync(PackagedDeploymentConfiguration packagedConfig, string environment, string adminPassword, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            ThrowIfMultipleMongoStrategiesAreInvalidCombination(packagedConfig.GetInitializationStrategiesOf<InitializationStrategyMongo>());

            ThrowIfCertificatesNotProperlyConfigured(packagedConfig.GetInitializationStrategiesOf<InitializationStrategyCertificateToInstall>(), packagedConfig.GetInitializationStrategiesOf<InitializationStrategySelfHost>(), packagedConfig.GetInitializationStrategiesOf<InitializationStrategyIis>());

            var ret = new List<SetupStepBatch>();

            var distinctInitializationStrategyTypes = packagedConfig.InitializationStrategies.Select(_ => _.GetType()).Distinct().ToList();

            // only copy the package byes if there are initialization strategies on the package that require it...
            if (distinctInitializationStrategyTypes.Any(_ => this.InitializationStrategyTypesThatNeedPackageBytes.Contains(_)))
            {
                var deployUnzippedFileStep = this.GetCopyAndUnzipPackageStep(packagedConfig);
                var batch = new SetupStepBatch { ExecutionOrder = 1, Steps = new[] { deployUnzippedFileStep } };
                ret.Add(batch);
            }

            foreach (var initializationStrategy in packagedConfig.InitializationStrategies)
            {
                var initSteps = await this.GetStrategySpecificSetupStepsAsync(initializationStrategy, packagedConfig, environment, adminPassword, funcToCreateNewDnsWithTokensReplaced);
                ret.Add(initSteps);
            }

            return ret;
        }

        private async Task<SetupStepBatch> GetStrategySpecificSetupStepsAsync(InitializationStrategyBase strategy, PackagedDeploymentConfiguration packagedConfig, string environment, string adminPassword, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            SetupStepBatch ret = null;
            var packageDirectoryPath = this.GetPackageDirectoryPath(packagedConfig);

            if (strategy.GetType() == typeof(InitializationStrategyDirectoryToCreate))
            {
                var dirSteps = this.GetDirectoryToCreateSpecificSteps(
                    (InitializationStrategyDirectoryToCreate)strategy,
                    this.settings.HarnessSettings.HarnessAccount,
                    this.settings.WebServerSettings.IisAccount);

                ret = new SetupStepBatch { ExecutionOrder = 2, Steps = dirSteps };
            }
            else if (strategy.GetType() == typeof(InitializationStrategyCertificateToInstall))
            {
                var certSteps =
                    await
                        this.GetCertificateToInstallSpecificStepsAsync(
                            (InitializationStrategyCertificateToInstall)strategy,
                            packageDirectoryPath,
                            this.settings.HarnessSettings.HarnessAccount,
                            this.settings.WebServerSettings.IisAccount);

                ret = new SetupStepBatch { ExecutionOrder = 3, Steps = certSteps };
            }
            else if (strategy.GetType() == typeof(InitializationStrategySqlServer))
            {
                var databaseSteps = this.GetSqlServerSpecificSteps((InitializationStrategySqlServer)strategy, packagedConfig.PackageWithBundleIdentifier.Package);

                ret = new SetupStepBatch { ExecutionOrder = 4, Steps = databaseSteps };
            }
            else if (strategy.GetType() == typeof(InitializationStrategyMongo))
            {
                var mongoSteps = this.GetMongoSpecificSteps((InitializationStrategyMongo)strategy);

                ret = new SetupStepBatch { ExecutionOrder = 5, Steps = mongoSteps };
            }
            else if (strategy.GetType() == typeof(InitializationStrategyMessageBusHandler))
            {
                /* No additional steps necessary as the DeploymentManager should have included a harness by virtue of this type of initialization strategy */
            }
            else if (strategy.GetType() == typeof(InitializationStrategyDnsEntry))
            {
                /* No additional steps necessary as the DeploymentManager performs this operation at the end */
            }
            else if (strategy.GetType() == typeof(InitializationStrategyScheduledTask))
            {
                var consoleRootPath = Path.Combine(packageDirectoryPath, "packagedConsoleApp"); // this needs to match how the package was built in the build system...
                var scheduledTaskSteps =
                    this.GetScheduledTaskSpecificSteps(
                        (InitializationStrategyScheduledTask)strategy,
                        packagedConfig.ItsConfigOverrides,
                        consoleRootPath,
                        environment,
                        adminPassword);

                ret = new SetupStepBatch { ExecutionOrder = 6, Steps = scheduledTaskSteps };
            }
            else if (strategy.GetType() == typeof(InitializationStrategySelfHost))
            {
                var consoleRootPath = Path.Combine(packageDirectoryPath, "packagedConsoleApp"); // this needs to match how the package was built in the build system...
                var selfHostSteps =
                    await
                    this.GetSelfHostSpecificSteps(
                        (InitializationStrategySelfHost)strategy,
                        packagedConfig.ItsConfigOverrides,
                        consoleRootPath,
                        environment,
                        adminPassword,
                        funcToCreateNewDnsWithTokensReplaced);

                ret = new SetupStepBatch { ExecutionOrder = 7, Steps = selfHostSteps };
            }
            else if (strategy.GetType() == typeof(InitializationStrategyIis))
            {
                var webRootPath = Path.Combine(packageDirectoryPath, "packagedWebsite"); // this needs to match how the package was built in the build system...
                var webSteps = await this.GetIisSpecificSetupStepsAsync(
                                   (InitializationStrategyIis)strategy,
                                   packagedConfig.ItsConfigOverrides,
                                   webRootPath,
                                   environment,
                                   adminPassword,
                                   funcToCreateNewDnsWithTokensReplaced);

                ret = new SetupStepBatch { ExecutionOrder = 8, Steps = webSteps };
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
            var unzipScript = this.settings.DeploymentScriptBlocks.UnzipFile.ScriptText;
            var unzipParams = new[] { packageFilePath, packageDirectoryPath };
            var deployUnzippedFileStep = new SetupStep
                                             {
                                                 Description =
                                                     "Push package file and unzip: "
                                                     + packagedConfig.PackageWithBundleIdentifier.Package.PackageDescription.GetIdDotVersionString(),
                                                 SetupFunc = machineManager =>
                                                     {
                                                         // don't push the null package...
                                                         if (
                                                             !string.Equals(
                                                                 packagedConfig.PackageWithBundleIdentifier.Package.PackageDescription.Id,
                                                                 PackageDescription.NullPackageId))
                                                         {
                                                             // in case we're in a retry scenario we should just overwrite...
                                                             const bool Overwrite = true;
                                                             machineManager.SendFile(
                                                                 packageFilePath,
                                                                 packagedConfig.PackageWithBundleIdentifier.Package.PackageFileBytes,
                                                                 false,
                                                                 Overwrite);
                                                             Log.Write(() => machineManager.RunScript(unzipScript, unzipParams));
                                                         }

                                                         return new dynamic[0];
                                                     },
                                             };

            return deployUnzippedFileStep;
        }

        private string GetPackageDirectoryPath(PackagedDeploymentConfiguration packagedConfig)
        {
            return Path.Combine(this.RootDeploymentPath, packagedConfig.PackageWithBundleIdentifier.Package.PackageDescription.Id);
        }

        private static void ThrowIfCertificatesNotProperlyConfigured(
            ICollection<InitializationStrategyCertificateToInstall> certificateToInstallStrategies,
            ICollection<InitializationStrategySelfHost> selfHostStrategies,
            ICollection<InitializationStrategyIis> iisStrategies)
        {
            Action<string, string> verifyCertificate = (certName, description) =>
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
                };

            foreach (var selfHost in selfHostStrategies)
            {
                verifyCertificate(selfHost.SslCertificateName, Invariant($"Self Host Exe: {selfHost.SelfHostExeName}"));
            }

            foreach (var iis in iisStrategies)
            {
                verifyCertificate(iis.SslCertificateName, Invariant($"IIS: {iis.PrimaryDns}"));
            }
        }
    }
}
