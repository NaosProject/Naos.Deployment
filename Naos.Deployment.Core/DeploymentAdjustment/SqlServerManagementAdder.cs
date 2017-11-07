// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlServerManagementAdder.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Naos.Database.MessageBus.Scheduler;
    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.TypeRepresentation;

    using Spritely.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Class to implement <see cref="AdjustDeploymentBase"/> to add message bus harness package when needed.
    /// </summary>
    public class SqlServerManagementAdder : AdjustDeploymentBase
    {
        /// <summary>
        /// Suffix to add to <see cref="InitializationStrategySqlServer.ManagementChannelName" /> for the FileJanitor handler.
        /// </summary>
        public const string FileJanitorChannelSuffix = "_file";

        /// <summary>
        /// Suffix to add to <see cref="InitializationStrategySqlServer.ManagementChannelName" /> for the SqlServer handler.
        /// </summary>
        public const string SqlServerChannelSuffix = "_sql";

        /// <summary>
        /// Reason for injecting the FileJanitorHandler due to finding a <see cref="InitializationStrategySqlServer" />.
        /// </summary>
        public const string ReasonStringFile = "Found a SqlServer initialization strategy; added File System management.";

        /// <summary>
        /// Reason for injecting the DatabaseHandler due to finding a <see cref="InitializationStrategySqlServer" />.
        /// </summary>
        public const string ReasonStringDatabase = "Found a SqlServer initialization strategy; added Database management.";

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerManagementAdder"/> class.
        /// </summary>
        /// <param name="sqlServerManagementConfiguration">Message bus harness configuration.</param>
        public SqlServerManagementAdder(SqlServerManagementConfiguration sqlServerManagementConfiguration)
        {
            this.SqlServerManagementConfiguration = sqlServerManagementConfiguration;
        }

        /// <summary>
        /// Gets the message bus handler harness configuration.
        /// </summary>
        public SqlServerManagementConfiguration SqlServerManagementConfiguration { get; private set; }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        public override bool IsMatch(IManageConfigFiles configFileManager, ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith)
        {
            // get all sql server initializations to know if we need a handler.
            var packagesWithSqlServerInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .WhereContainsInitializationStrategyOf<InitializationStrategySqlServer>();

            var sqlServerInitializations =
                packagesWithSqlServerInitializations.GetInitializationStrategiesOf<InitializationStrategySqlServer>();

            var match = sqlServerInitializations.Any();

            return match;
        }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        public override IReadOnlyCollection<InjectedPackage> GetAdditionalPackages(string environment, string instanceName, int instanceNumber, IManageConfigFiles configFileManager, ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith, PackageHelper packageHelper, string[] itsConfigPrecedenceAfterEnvironment, SetupStepFactorySettings setupStepFactorySettings)
        {
            new { configFileManager }.Must().NotBeNull().OrThrowFirstFailure();
            new { packageHelper }.Must().NotBeNull().OrThrowFirstFailure();
            new { setupStepFactorySettings }.Must().NotBeNull().OrThrowFirstFailure();

            // get all sql server initializations to know if we need a handler.
            var packagesWithSqlServerInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .WhereContainsInitializationStrategyOf<InitializationStrategySqlServer>();

            var sqlServerInitializations = packagesWithSqlServerInitializations.GetInitializationStrategiesOf<InitializationStrategySqlServer>()
                .Where(_ => !string.IsNullOrWhiteSpace(_.ManagementChannelName)).ToList();

            var ret = new List<InjectedPackage>();

            if (!sqlServerInitializations.Any())
            {
                // if we don't have any management channel names specified then just deploy as usual
                return ret;
            }

            const int MaxManagementChannelNameLength = 23;
            var badManagementChannelNames = sqlServerInitializations.Where(_ => _.ManagementChannelName.Length > MaxManagementChannelNameLength).Select(_ => _.ManagementChannelName).ToList();
            if (badManagementChannelNames.Any())
            {
                throw new ArgumentException(Invariant($"Cannot have {nameof(InitializationStrategySqlServer)}.{nameof(InitializationStrategySqlServer.ManagementChannelName)} longer than {MaxManagementChannelNameLength}, failing ones are; {string.Join(",", badManagementChannelNames)}"));
            }

            foreach (var packageWithSqlServerInitialization in packagesWithSqlServerInitializations)
            {
                var itsConfigOverrides = new List<ItsConfigOverride>(packageWithSqlServerInitialization.ItsConfigOverrides ?? new List<ItsConfigOverride>());

                // extract appropriate files from
                var itsConfigFilesFromPackage = new Dictionary<string, string>();
                var precedenceChain = new[] { environment }.ToList();
                precedenceChain.AddRange(itsConfigPrecedenceAfterEnvironment);
                foreach (var precedenceElement in precedenceChain)
                {
                    var itsConfigFolderPattern = Invariant($"{setupStepFactorySettings.ConfigDirectory}/{precedenceElement}/");

                    var itsConfigFilesFromPackageForPrecedenceElement =
                        packageHelper.GetMultipleFileContentsFromPackageAsStrings(
                            packageWithSqlServerInitialization.PackageWithBundleIdentifier.Package,
                            itsConfigFolderPattern);

                    foreach (var item in itsConfigFilesFromPackageForPrecedenceElement)
                    {
                        itsConfigFilesFromPackage.Add(item.Key, item.Value);
                    }
                }

                itsConfigOverrides.AddRange(
                    itsConfigFilesFromPackage.Select(
                        _ => new ItsConfigOverride { FileNameWithoutExtension = Path.GetFileNameWithoutExtension(_.Key), FileContentsJson = _.Value }));

                var fileJanitor = this.BuildHandlerPackage(
                    environment,
                    instanceName,
                    instanceNumber,
                    configFileManager,
                    sqlServerInitializations,
                    itsConfigOverrides,
                    configToCreateWith,
                    packageHelper,
                    this.SqlServerManagementConfiguration.FileSystemManagementPackage,
                    FileJanitorChannelSuffix,
                    this.SqlServerManagementConfiguration.FileSystemManagementLogProcessorSettings);

                ret.Add(new InjectedPackage(ReasonStringFile, fileJanitor));

                var databaseConfig = new ItsConfigOverride
                                         {
                                             FileNameWithoutExtension = nameof(DatabaseMessageHandlerSettings),
                                             FileContentsJson =
                                                 configFileManager.SerializeConfigToFileText(
                                                     new DatabaseMessageHandlerSettings
                                                         {
                                                             DefaultTimeout = setupStepFactorySettings.DatabaseServerSettings.DefaultTimeout,
                                                             LocalhostConnectionString = sqlServerInitializations.Select(_ => _.CreateLocalhostConnectionString()).Distinct().Single(),
                                                             DataDirectory =
                                                                 sqlServerInitializations
                                                                     .Select(_ => _.DataDirectory).Distinct()
                                                                     .SingleOrDefault()
                                                                 ?? setupStepFactorySettings
                                                                     .DatabaseServerSettings
                                                                     .DefaultDataDirectory,
                                                             BackupDirectory =
                                                                 sqlServerInitializations
                                                                     .Select(_ => _.BackupDirectory).Distinct()
                                                                     .SingleOrDefault()
                                                                 ?? setupStepFactorySettings
                                                                     .DatabaseServerSettings
                                                                     .DefaultBackupDirectory,
                                                         }),
                                         };

                var databaseHandler = this.BuildHandlerPackage(
                    environment,
                    instanceName,
                    instanceNumber,
                    configFileManager,
                    sqlServerInitializations,
                    itsConfigOverrides.Concat(new[] { databaseConfig }).ToList(),
                    configToCreateWith,
                    packageHelper,
                    this.SqlServerManagementConfiguration.SqlServerManagementPackage,
                    SqlServerChannelSuffix,
                    this.SqlServerManagementConfiguration.SqlServerManagementLogProcessorSettings);

                ret.Add(new InjectedPackage(ReasonStringDatabase, databaseHandler));
            }

            return ret;
        }

        private PackagedDeploymentConfiguration BuildHandlerPackage(string environment, string instanceName, int instanceNumber, IManageConfigFiles configFileManager, ICollection<InitializationStrategySqlServer> sqlServerInitializations, IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides, DeploymentConfiguration configToCreateWith, PackageHelper packageHelper, PackageDescriptionWithOverrides packageDescriptionToAdd, string channelSuffix, LogProcessorSettings logProcessorSettings)
        {
            // Create a new list to use for the overrides of the handler harness deployment
            var itsConfigOverridesToUse = new List<ItsConfigOverride>();
            if (itsConfigOverrides != null)
            {
                // merge in any ItsConfig overrides supplied with handler packages
                itsConfigOverridesToUse.AddRange(itsConfigOverrides);
            }

            if (packageDescriptionToAdd.ItsConfigOverrides != null)
            {
                // merge in any overrides specified with the handler package
                itsConfigOverridesToUse.AddRange(packageDescriptionToAdd.ItsConfigOverrides);
            }

            var packageToAdd = packageHelper.GetPackage(packageDescriptionToAdd, false);

            var actualVersion = packageHelper.GetActualVersionFromPackage(packageToAdd.Package);
            packageToAdd.Package.PackageDescription.Version = actualVersion;

            var adjustedChannelsToMonitor =
                sqlServerInitializations
                    .Select(_ => new SimpleChannel(TokenSubstitutions.GetSubstitutedStringForChannelName(_.ManagementChannelName + channelSuffix, environment, instanceName, instanceNumber)))
                    .Cast<IChannel>()
                    .ToList();

            var launchConfig = new MessageBusLaunchConfiguration(
                this.SqlServerManagementConfiguration.HandlerHarnessProcessTimeToLive,
                TypeMatchStrategy.NamespaceAndName,
                TypeMatchStrategy.NamespaceAndName,
                0,
                TimeSpan.FromMinutes(1),
                1,
                adjustedChannelsToMonitor);

            var handlerFactoryConfig = new HandlerFactoryConfiguration(TypeMatchStrategy.NamespaceAndName);

            itsConfigOverridesToUse.AddRange(
                new[]
                    {
                        new ItsConfigOverride
                            {
                                FileNameWithoutExtension = nameof(MessageBusLaunchConfiguration),
                                FileContentsJson = configFileManager.SerializeConfigToFileText(launchConfig),
                            },
                        new ItsConfigOverride
                            {
                                FileNameWithoutExtension = nameof(HandlerFactoryConfiguration),
                                FileContentsJson = configFileManager.SerializeConfigToFileText(handlerFactoryConfig),
                            },
                        new ItsConfigOverride
                            {
                                FileNameWithoutExtension = nameof(MessageBusConnectionConfiguration),
                                FileContentsJson = configFileManager.SerializeConfigToFileText(this.SqlServerManagementConfiguration.PersistenceConnectionConfiguration),
                            },
                        new ItsConfigOverride
                            {
                                FileNameWithoutExtension = nameof(LogProcessorSettings),
                                FileContentsJson = configFileManager.SerializeConfigToFileText(logProcessorSettings),
                            },
                    });

            var existingInitializationStrategies = packageDescriptionToAdd.InitializationStrategies.Select(_ => (InitializationStrategyBase)_.Clone()).ToList();

            var harnessPackagedConfig = new PackagedDeploymentConfiguration
            {
                DeploymentConfiguration = configToCreateWith,
                PackageWithBundleIdentifier = packageToAdd,
                ItsConfigOverrides = itsConfigOverridesToUse,
                InitializationStrategies = existingInitializationStrategies,
            };

            return harnessPackagedConfig;
        }
    }
}