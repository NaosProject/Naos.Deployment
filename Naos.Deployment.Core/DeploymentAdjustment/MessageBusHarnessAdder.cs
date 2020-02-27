// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageBusHarnessAdder.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Representation.System;

    /// <summary>
    /// Class to implement <see cref="AdjustDeploymentBase"/> to add message bus harness package when needed.
    /// </summary>
    public class MessageBusHarnessAdder : AdjustDeploymentBase
    {
        /// <summary>
        /// Reason for injecting the harness due to finding a message bus initialization strategy.
        /// </summary>
        public const string ReasonString = "Found a message bus initialization strategy.";

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBusHarnessAdder"/> class.
        /// </summary>
        /// <param name="messageBusHandlerHarnessConfiguration">Message bus harness configuration.</param>
        public MessageBusHarnessAdder(MessageBusHandlerHarnessConfiguration messageBusHandlerHarnessConfiguration)
        {
            this.MessageBusHandlerHarnessConfiguration = messageBusHandlerHarnessConfiguration;
        }

        /// <summary>
        /// Gets the message bus handler harness configuration.
        /// </summary>
        public MessageBusHandlerHarnessConfiguration MessageBusHandlerHarnessConfiguration { get; private set; }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        public override bool IsMatch(IManageConfigFiles configFileManager, IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith)
        {
            // get all message bus handler initializations to know if we need a handler.
            var packagesWithMessageBusInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .WhereContainsInitializationStrategyOf<InitializationStrategyMessageBusHandler>();

            var messageBusInitializations =
                packagesWithMessageBusInitializations.GetInitializationStrategiesOf<InitializationStrategyMessageBusHandler>();

            // make sure we're not already deploying the package ('server/host/schedule manager' is only scenario of this right now...)
            var alreadyDeployingTheSamePackageAsHandlersUse =
                packagedDeploymentConfigsWithDefaultsAndOverrides.Any(
                    _ => _.PackageWithBundleIdentifier.Package.PackageDescription.Id == this.MessageBusHandlerHarnessConfiguration.Package.PackageDescription.Id);

            var hasMessageBusInitializations = messageBusInitializations.Any();
            var match = hasMessageBusInitializations && !alreadyDeployingTheSamePackageAsHandlersUse;

            return match;
        }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        public override IReadOnlyCollection<InjectedPackage> GetAdditionalPackages(string environment, string instanceName, int instanceNumber, IManageConfigFiles configFileManager, IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith, PackageHelper packageHelper, SetupStepFactorySettings setupStepFactorySettings)
        {
            new { configFileManager }.AsArg().Must().NotBeNull();
            new { configToCreateWith }.AsArg().Must().NotBeNull();
            new { packageHelper }.AsArg().Must().NotBeNull();
            new { setupStepFactorySettings }.AsArg().Must().NotBeNull();

            PackagedDeploymentConfiguration ret = null;

            // get all message bus handler initializations to know if we need a handler.
            var packagesWithMessageBusInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .WhereContainsInitializationStrategyOf<InitializationStrategyMessageBusHandler>();

            var messageBusInitializations =
                packagesWithMessageBusInitializations.GetInitializationStrategiesOf<InitializationStrategyMessageBusHandler>();

            var itsConfigOverridesForHandlers = new List<ItsConfigOverride>();

            foreach (var packageWithMessageBusInitializations in packagesWithMessageBusInitializations)
            {
                itsConfigOverridesForHandlers.AddRange(packageWithMessageBusInitializations.ItsConfigOverrides ?? new List<ItsConfigOverride>());

                // extract appropriate files from
                var itsConfigFilesFromPackage = new Dictionary<string, string>();
                var precedenceChain = configFileManager.BuildPrecedenceChain(environment);
                foreach (var precedenceElement in precedenceChain)
                {
                    var itsConfigFolderPattern = configFileManager.BuildConfigPath(precedence: precedenceElement);

                    var itsConfigFilesFromPackageForPrecedenceElement =
                        packageHelper.GetMultipleFileContentsFromPackageAsStrings(
                            packageWithMessageBusInitializations.PackageWithBundleIdentifier.Package,
                            itsConfigFolderPattern);

                    foreach (var item in itsConfigFilesFromPackageForPrecedenceElement)
                    {
                        itsConfigFilesFromPackage.Add(item.Key, item.Value);
                    }
                }

                itsConfigOverridesForHandlers.AddRange(
                    itsConfigFilesFromPackage.Select(
                        _ => new ItsConfigOverride { FileNameWithoutExtension = Path.GetFileNameWithoutExtension(_.Key), FileContentsJson = _.Value }));

                var rootDeploymentPath = setupStepFactorySettings.BuildRootDeploymentPath(configToCreateWith.Volumes);
                ret = this.BuildMessageBusHarnessPackagedConfig(
                    environment,
                    instanceName,
                    instanceNumber,
                    configFileManager,
                    messageBusInitializations,
                    itsConfigOverridesForHandlers,
                    configToCreateWith,
                    packageHelper,
                    rootDeploymentPath);
            }

            return ret == null ? new InjectedPackage[0] : new[] { new InjectedPackage(ReasonString, ret) };
        }

        private PackagedDeploymentConfiguration BuildMessageBusHarnessPackagedConfig(string environment, string instanceName, int instanceNumber, IManageConfigFiles configFileManager, IReadOnlyCollection<InitializationStrategyMessageBusHandler> messageBusInitializations, IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides, DeploymentConfiguration configToCreateWith, PackageHelper packageHelper, string rootDeploymentPath)
        {
            // TODO:    Maybe this should be exclusively done with that provided package and
            // TODO:        only update the private channel to monitor and directory of packages...

            // Create a new list to use for the overrides of the handler harness deployment
            var itsConfigOverridesToUse = new List<ItsConfigOverride>();
            if (itsConfigOverrides != null)
            {
                // merge in any ItsConfig overrides supplied with handler packages
                itsConfigOverridesToUse.AddRange(itsConfigOverrides);
            }

            if (this.MessageBusHandlerHarnessConfiguration.Package.ItsConfigOverrides != null)
            {
                // merge in any overrides specified with the handler package
                itsConfigOverridesToUse.AddRange(this.MessageBusHandlerHarnessConfiguration.Package.ItsConfigOverrides);
            }

            var messageBusHandlerPackage = packageHelper.GetPackage(this.MessageBusHandlerHarnessConfiguration.Package.PackageDescription, false);

            var actualVersion = packageHelper.GetActualVersionFromPackage(messageBusHandlerPackage.Package);
            messageBusHandlerPackage.Package.PackageDescription.Version = actualVersion;

            var channelsToMonitor = messageBusInitializations.SelectMany(_ => _.ChannelsToMonitor).Distinct().ToList();

            // recreate channels with channel name substitutions
            var simpleChannels = channelsToMonitor.OfType<SimpleChannel>().ToList();
            if (simpleChannels.Count != channelsToMonitor.Count)
            {
                throw new ArgumentException("There are IChannels that are NOT SimpleChannels and this is not supported for the token substitution.");
            }

            var adjustedChannelsToMonitor =
                simpleChannels
                    .Select(_ => new SimpleChannel(TokenSubstitutions.GetSubstitutedStringForChannelName(_.Name, environment, instanceName, instanceNumber)))
                    .Cast<IChannel>()
                    .ToList();

            var workerCount = messageBusInitializations.Max(_ => _.WorkerCount);
            workerCount = workerCount == 0 ? 1 : workerCount;

            var launchConfig = new MessageBusLaunchConfiguration(
                this.MessageBusHandlerHarnessConfiguration.HandlerHarnessProcessTimeToLive,
                TypeMatchStrategy.NamespaceAndName,
                TypeMatchStrategy.NamespaceAndName,
                0,
                TimeSpan.FromMinutes(1),
                workerCount,
                adjustedChannelsToMonitor);

            var handlerFactoryConfig = new HandlerFactoryConfiguration(TypeMatchStrategy.NamespaceAndName, rootDeploymentPath);

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
                                FileContentsJson = configFileManager.SerializeConfigToFileText(this.MessageBusHandlerHarnessConfiguration.PersistenceConnectionConfiguration),
                            },
                    });

            if (this.MessageBusHandlerHarnessConfiguration.LogWritingSettings != null)
            {
                itsConfigOverridesToUse.Add(
                    new ItsConfigOverride
                        {
                            FileNameWithoutExtension = nameof(LogWritingSettings),
                            FileContentsJson = configFileManager.SerializeConfigToFileText(this.MessageBusHandlerHarnessConfiguration.LogWritingSettings),
                        });
            }

            var messageBusHandlerHarnessInitializationStrategies = this.MessageBusHandlerHarnessConfiguration.Package.InitializationStrategies.Select(_ => (InitializationStrategyBase)_.Clone()).ToList();

            var harnessPackagedConfig = new PackagedDeploymentConfiguration
                                            {
                                                DeploymentConfiguration = configToCreateWith,
                                                PackageWithBundleIdentifier = messageBusHandlerPackage,
                                                ItsConfigOverrides = itsConfigOverridesToUse,
                                                InitializationStrategies = messageBusHandlerHarnessInitializationStrategies,
                                            };

            return harnessPackagedConfig;
        }
    }
}
