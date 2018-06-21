// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DatabaseManagementAdder.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Database.Domain;
    using Naos.Database.MessageBus.Scheduler;
    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.TypeRepresentation;
    using OBeautifulCode.Validation.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Class to implement <see cref="AdjustDeploymentBase"/> to add message bus harness package when needed.
    /// </summary>
    public class DatabaseManagementAdder : AdjustDeploymentBase
    {
        /// <summary>
        /// Suffix to add to <see cref="InitializationStrategySqlServer.ManagementChannelName" /> or <see cref="InitializationStrategyMongo.ManagementChannelName" /> for the FileJanitor handler.
        /// </summary>
        public const string FileJanitorChannelSuffix = "_file";

        /// <summary>
        /// Suffix to add to <see cref="InitializationStrategySqlServer.ManagementChannelName" /> or <see cref="InitializationStrategyMongo.ManagementChannelName" /> for the Database handler.
        /// </summary>
        public const string DatabaseChannelSuffix = "_db";

        /// <summary>
        /// Reason for injecting the FileJanitorHandler due to finding a <see cref="InitializationStrategySqlServer" /> or <see cref="InitializationStrategyMongo" />.
        /// </summary>
        public const string ReasonStringFile = "Found a Database initialization strategy; added File System management.";

        /// <summary>
        /// Reason for injecting the DatabaseHandler due to finding a <see cref="InitializationStrategySqlServer" /> or <see cref="InitializationStrategyMongo" />.
        /// </summary>
        public const string ReasonStringDatabase = "Found a Database initialization strategy; added Database management.";

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseManagementAdder"/> class.
        /// </summary>
        /// <param name="databaseManagementConfiguration">Message bus harness configuration.</param>
        public DatabaseManagementAdder(DatabaseManagementConfiguration databaseManagementConfiguration)
        {
            this.DatabaseManagementConfiguration = databaseManagementConfiguration;
        }

        /// <summary>
        /// Gets the message bus handler harness configuration.
        /// </summary>
        public DatabaseManagementConfiguration DatabaseManagementConfiguration { get; private set; }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        public override bool IsMatch(IManageConfigFiles configFileManager, IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith)
        {
            var sqlServerInitializations = packagedDeploymentConfigsWithDefaultsAndOverrides
                .WhereContainsInitializationStrategyOf<InitializationStrategySqlServer>().GetInitializationStrategiesOf<InitializationStrategySqlServer>();

            var mongoInitializations = packagedDeploymentConfigsWithDefaultsAndOverrides
                .WhereContainsInitializationStrategyOf<InitializationStrategyMongo>().GetInitializationStrategiesOf<InitializationStrategyMongo>();

            var match = sqlServerInitializations.Any() || mongoInitializations.Any();

            return match;
        }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Like it this way.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        public override IReadOnlyCollection<InjectedPackage> GetAdditionalPackages(string environment, string instanceName, int instanceNumber, IManageConfigFiles configFileManager, IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith, PackageHelper packageHelper, SetupStepFactorySettings setupStepFactorySettings)
        {
            new { configFileManager }.Must().NotBeNull();
            new { packageHelper }.Must().NotBeNull();
            new { setupStepFactorySettings }.Must().NotBeNull();

            var sqlServerInitializations = packagedDeploymentConfigsWithDefaultsAndOverrides
                .WhereContainsInitializationStrategyOf<InitializationStrategySqlServer>().GetInitializationStrategiesOf<InitializationStrategySqlServer>()
                .Where(_ => !string.IsNullOrWhiteSpace(_.ManagementChannelName)).ToList();

            var mongoInitializations = packagedDeploymentConfigsWithDefaultsAndOverrides
                .WhereContainsInitializationStrategyOf<InitializationStrategyMongo>().GetInitializationStrategiesOf<InitializationStrategyMongo>();

            var databaseNameToAdminPasswordMap = sqlServerInitializations.ToDictionary(k => k.Name, v => v.AdministratorPassword)
                .Union(mongoInitializations.ToDictionary(k => k.DocumentDatabaseName, v => v.AdministratorPassword));

            var ret = new List<InjectedPackage>();

            if (!sqlServerInitializations.Any() && !mongoInitializations.Any())
            {
                // if we don't have any management channel names specified then just deploy as usual
                return ret;
            }

            var managementChannelNames = sqlServerInitializations.Cast<IHaveManagementChannel>().Concat(mongoInitializations).Select(
                _ => _.ManagementChannelName ?? throw new ArgumentException(
                         Invariant($"Must specify a {nameof(_.ManagementChannelName)} for initialization: {_}(type - {_.GetType()})"))).Distinct().ToList();

            const int MaxManagementChannelNameLength = 23;
            var badManagementChannelNames = managementChannelNames.Where(_ => _.Length > MaxManagementChannelNameLength).Select(_ => _).ToList();
            if (badManagementChannelNames.Any())
            {
                throw new ArgumentException(Invariant($"Cannot have {nameof(IHaveManagementChannel.ManagementChannelName)} longer than {MaxManagementChannelNameLength}, failing ones are; {string.Join(",", badManagementChannelNames)}"));
            }

            var fileJanitor = this.BuildHandlerPackage(
                environment,
                instanceName,
                instanceNumber,
                configFileManager,
                managementChannelNames,
                new ItsConfigOverride[0],
                new InitializationStrategyBase[0],
                configToCreateWith,
                packageHelper,
                this.DatabaseManagementConfiguration.FileSystemManagementPackage,
                FileJanitorChannelSuffix,
                this.DatabaseManagementConfiguration.FileSystemManagementLogWritingSettings);

            ret.Add(new InjectedPackage(ReasonStringFile, fileJanitor));

            var databaseKindToDataDirectoryMap =
                new Dictionary<DatabaseKind, string>
                    {
                        {
                            DatabaseKind.SqlServer,
                            sqlServerInitializations.Select(_ => _.DataDirectory).Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct().SingleOrDefault()
                            ?? setupStepFactorySettings.DatabaseServerSettings.DefaultDataDirectory
                        },
                        {
                            DatabaseKind.Mongo,
                            sqlServerInitializations.Select(_ => _.DataDirectory).Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct().SingleOrDefault()
                            ?? setupStepFactorySettings.MongoServerSettings.DefaultDataDirectory
                        },
                    };

            var databaseKindToBackupDirectoryMap =
                new Dictionary<DatabaseKind, string>
                    {
                        {
                            DatabaseKind.SqlServer,
                            sqlServerInitializations.Select(_ => _.BackupDirectory).Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct().SingleOrDefault()
                            ?? setupStepFactorySettings.DatabaseServerSettings.DefaultBackupDirectory
                        },
                        {
                            DatabaseKind.Mongo,
                            sqlServerInitializations.Select(_ => _.BackupDirectory).Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct().SingleOrDefault()
                            ?? setupStepFactorySettings.MongoServerSettings.DefaultBackupDirectory
                        },
                    };

            var databaseNameToLocalhostConnectionDefinitionMap = databaseNameToAdminPasswordMap.ToDictionary(
                k => k.Key.ToUpperInvariant(),
                v => new ConnectionDefinition { Server = "localhost", DatabaseName = v.Key, UserName = "sa", Password = v.Value, });

            var databaseMessageHandlerSettings =
                new DatabaseMessageHandlerSettings
                    {
                        DefaultTimeout = setupStepFactorySettings.DatabaseServerSettings.DefaultTimeout,
                        DatabaseNameToLocalhostConnectionDefinitionMap = databaseNameToLocalhostConnectionDefinitionMap,
                        DatabaseKindToDataDirectoryMap = databaseKindToDataDirectoryMap,
                        DatabaseKindToBackupDirectoryMap = databaseKindToBackupDirectoryMap,
                        MongoUtilityDirectory = setupStepFactorySettings.MongoServerSettings.DefaultUtilityDirectory,
                        WorkingDirectoryPath = setupStepFactorySettings.MongoServerSettings.DefaultWorkingDirectory,
                    };

            var databaseConfig = new ItsConfigOverride
                                     {
                                         FileNameWithoutExtension = nameof(DatabaseMessageHandlerSettings),
                                         FileContentsJson = configFileManager.SerializeConfigToFileText(databaseMessageHandlerSettings),
                                     };

            var initializationStrategyDirectoryToCreate =
                new InitializationStrategyDirectoryToCreate
                    {
                        DirectoryToCreate =
                            new DirectoryToCreateDetails
                                {
                                    FullPath = databaseMessageHandlerSettings.WorkingDirectoryPath,
                                    FullControlAccount = setupStepFactorySettings.MongoServerSettings.ServiceAccount,
                                },
                    };

            var databaseHandler = this.BuildHandlerPackage(
                environment,
                instanceName,
                instanceNumber,
                configFileManager,
                managementChannelNames,
                new[] { databaseConfig },
                new[] { initializationStrategyDirectoryToCreate },
                configToCreateWith,
                packageHelper,
                this.DatabaseManagementConfiguration.DatabaseManagementPackage,
                DatabaseChannelSuffix,
                this.DatabaseManagementConfiguration.DatabaseManagementLogWritingSettings);

            ret.Add(new InjectedPackage(ReasonStringDatabase, databaseHandler));

            return ret;
        }

        private PackagedDeploymentConfiguration BuildHandlerPackage(
            string environment,
            string instanceName,
            int instanceNumber,
            IManageConfigFiles configFileManager,
            IReadOnlyCollection<string> managementChannels,
            IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides,
            IReadOnlyCollection<InitializationStrategyBase> initializationStrategiesToAdd,
            DeploymentConfiguration configToCreateWith,
            PackageHelper packageHelper,
            PackageDescriptionWithOverrides packageDescriptionToAdd,
            string channelSuffix,
            LogWritingSettings logWritingSettings)
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
                managementChannels
                    .Select(_ => new SimpleChannel(TokenSubstitutions.GetSubstitutedStringForChannelName(_ + channelSuffix, environment, instanceName, instanceNumber)))
                    .Cast<IChannel>()
                    .ToList();

            var launchConfig = new MessageBusLaunchConfiguration(
                this.DatabaseManagementConfiguration.HandlerHarnessProcessTimeToLive,
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
                                FileContentsJson = configFileManager.SerializeConfigToFileText(this.DatabaseManagementConfiguration.PersistenceConnectionConfiguration),
                            },
                    });

            if (logWritingSettings != null)
            {
                itsConfigOverridesToUse.Add(
                    new ItsConfigOverride
                        {
                            FileNameWithoutExtension = nameof(logWritingSettings),
                            FileContentsJson = configFileManager.SerializeConfigToFileText(logWritingSettings),
                        });
            }

            var existingInitializationStrategies = packageDescriptionToAdd.InitializationStrategies.Select(_ => (InitializationStrategyBase)_.Clone()).ToList();

            var harnessPackagedConfig = new PackagedDeploymentConfiguration
                                            {
                                                DeploymentConfiguration = configToCreateWith,
                                                PackageWithBundleIdentifier = packageToAdd,
                                                ItsConfigOverrides = itsConfigOverridesToUse,
                                                InitializationStrategies = existingInitializationStrategies
                                                    .Concat(initializationStrategiesToAdd ?? new List<InitializationStrategyBase>())
                                                    .ToList(),
                                            };

            return harnessPackagedConfig;
        }
    }
}