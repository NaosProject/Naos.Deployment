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

    using Its.Log.Instrumentation;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private readonly IGetCertificates certificateRetriever;

        private readonly SetupStepFactorySettings settings;

        private readonly IGetPackages packageManager;

        private readonly string[] itsConfigPrecedenceAfterEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetupStepFactory"/> class.
        /// </summary>
        /// <param name="settings">Settings for the factory.</param>
        /// <param name="certificateRetriever">Certificate retriever to get certificates for steps.</param>
        /// <param name="packageManager">Package manager to use for getting package files contents.</param>
        /// <param name="itsConfigPrecedenceAfterEnvironment">Its.Config precedence chain to be applied after the environment during any setup steps concerned with it.</param>
        public SetupStepFactory(SetupStepFactorySettings settings, IGetCertificates certificateRetriever, IGetPackages packageManager, string[] itsConfigPrecedenceAfterEnvironment)
        {
            this.certificateRetriever = certificateRetriever;
            this.settings = settings;
            this.packageManager = packageManager;
            this.itsConfigPrecedenceAfterEnvironment = itsConfigPrecedenceAfterEnvironment;
        }

        /// <summary>
        /// Gets the administrator account name.
        /// </summary>
        public string AdministratorAccount
        {
            get
            {
                return this.settings.AdministratorAccount;
            }
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
        /// Gets the initialization strategy types that require the package bytes to be copied up to the target server.
        /// </summary>
        public IReadOnlyCollection<Type> InitializationStrategyTypesThatNeedPackageBytes
        {
            get
            {
                return this.settings.InitializationStrategyTypesThatNeedPackageBytes;
            }
        }

        /// <summary>
        /// Gets the max number of times to execute a setup step before throwing.
        /// </summary>
        public int MaxSetupStepAttempts
        {
            get
            {
                return this.settings.MaxSetupStepAttempts;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not to throw if the max attempts are not successful on a setup step.
        /// </summary>
        public bool ThrowOnFailedSetupStep
        {
            get
            {
                return this.settings.ThrowOnFailedSetupStep;
            }
        }

        /// <summary>
        /// Get the appropriate setup steps for the packaged config.
        /// </summary>
        /// <param name="packagedConfig">Config to base setup steps from.</param>
        /// <param name="environment">Environment that is being deployed.</param>
        /// <param name="adminPassword">Administrator password for the machine in case an application needs to be run as that user (which is discouraged!).</param>
        /// <returns>Collection of setup steps that will leave the machine properly configured.</returns>
        public ICollection<SetupStep> GetSetupSteps(PackagedDeploymentConfiguration packagedConfig, string environment, string adminPassword)
        {
            // Make sure we only have one data,log path, and journaling option because mongo uses a single config file for this
            var mongoStrategies = packagedConfig.GetInitializationStrategiesOf<InitializationStrategyMongo>();
            var distinctNoJournaling = mongoStrategies.Select(_ => _.NoJournaling).Distinct();
            if (distinctNoJournaling.Count() > 1)
            {
                throw new ArgumentException("Cannot have multiple no journaling options for a single mongo instance deployment.");
            }

            var distinctDataPath = mongoStrategies.Select(_ => _.DataDirectory).Distinct();
            if (distinctDataPath.Count() > 1)
            {
                throw new ArgumentException("Cannot have multiple data paths for a single mongo instance deployment.");
            }

            var distinctLogPath = mongoStrategies.Select(_ => _.LogDirectory).Distinct();
            if (distinctLogPath.Count() > 1)
            {
                throw new ArgumentException("Cannot have multiple log paths for a single mongo instance deployment.");
            }

            var ret = new List<SetupStep>();

            var installChocoStep = this.GetChocolateySetupStep(packagedConfig);
            if (installChocoStep != null)
            {
                ret.Add(installChocoStep);
            }

            var distinctInitializationStrategyTypes =
                packagedConfig.InitializationStrategies.Select(_ => _.GetType()).Distinct().ToList();

            // only copy the package byes if there are initialization strategies on the package that require it...
            if (distinctInitializationStrategyTypes.Any(_ => this.InitializationStrategyTypesThatNeedPackageBytes.Contains(_)))
            {
                var deployUnzippedFileStep = this.GetCopyAndUnzipPackageStep(packagedConfig);
                ret.Add(deployUnzippedFileStep);
            }

            foreach (var initializationStrategy in packagedConfig.InitializationStrategies)
            {
                var initSteps = this.GetStrategySpecificSetupSteps(initializationStrategy, packagedConfig, environment, adminPassword);
                ret.AddRange(initSteps);
            }

            return ret;
        }

        private ICollection<SetupStep> GetStrategySpecificSetupSteps(InitializationStrategyBase strategy, PackagedDeploymentConfiguration packagedConfig, string environment, string adminPassword)
        {
            var ret = new List<SetupStep>();
            var packageDirectoryPath = this.GetPackageDirectoryPath(packagedConfig);

            if (strategy.GetType() == typeof(InitializationStrategyIis))
            {
                var webRootPath = Path.Combine(packageDirectoryPath, "packagedWebsite"); // this needs to match how the package was built in the build system...
                var webSteps = this.GetIisSpecificSetupSteps(
                    (InitializationStrategyIis)strategy,
                    packagedConfig.ItsConfigOverrides,
                    packageDirectoryPath,
                    webRootPath,
                    environment,
                    adminPassword);
                ret.AddRange(webSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategySqlServer))
            {
                var databaseSteps = this.GetSqlServerSpecificSteps((InitializationStrategySqlServer)strategy, packagedConfig.Package);
                ret.AddRange(databaseSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyMongo))
            {
                var databaseSteps = this.GetMongoSpecificSteps((InitializationStrategyMongo)strategy);
                ret.AddRange(databaseSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyMessageBusHandler))
            {
                /* No additional steps necessary as the DeploymentManager should have included a harness by virtue of this type of initialization strategy */
            }
            else if (strategy.GetType() == typeof(InitializationStrategyDnsEntry))
            {
                /* No additional steps necessary as the DeploymentManager performs this operation at the end */
            }
            else if (strategy.GetType() == typeof(InitializationStrategyDirectoryToCreate))
            {
                var dirSteps = this.GetDirectoryToCreateSpecificSteps(
                    (InitializationStrategyDirectoryToCreate)strategy,
                    this.settings.HarnessSettings.HarnessAccount,
                    this.settings.WebServerSettings.IisAccount);
                ret.AddRange(dirSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyCertificateToInstall))
            {
                var certSteps =
                    this.GetCertificateToInstallSpecificSteps(
                        (InitializationStrategyCertificateToInstall)strategy,
                        packageDirectoryPath,
                        this.settings.HarnessSettings.HarnessAccount,
                        this.settings.WebServerSettings.IisAccount);
                ret.AddRange(certSteps);
            }
            else if (strategy.GetType() == typeof(InitializationStrategyScheduledTask))
            {
                var consoleRootPath = Path.Combine(packageDirectoryPath, "packagedConsoleApp"); // this needs to match how the package was built in the build system...
                var scheduledTaskSteps =
                    this.GetScheduledTaskSpecificSteps(
                        (InitializationStrategyScheduledTask)strategy,
                        packagedConfig.ItsConfigOverrides,
                        consoleRootPath,
                        environment);
                ret.AddRange(scheduledTaskSteps);
            }
            else
            {
                throw new DeploymentException("The initialization strategy type is not supported: " + strategy.GetType());
            }

            return ret;
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
                                                                 PackageDescription.NullPackageId))
                                                         {
                                                             // in case we're in a retry scenario we should just overwrite...
                                                             const bool Overwrite = true;
                                                             machineManager.SendFile(
                                                                 packageFilePath,
                                                                 packagedConfig.Package.PackageFileBytes,
                                                                 false,
                                                                 Overwrite);
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

                                                   machineManager.RunScript(
                                                       this.settings.DeploymentScriptBlocks.InstallChocolatey
                                                           .ScriptText,
                                                       scriptBlockParameters);
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
