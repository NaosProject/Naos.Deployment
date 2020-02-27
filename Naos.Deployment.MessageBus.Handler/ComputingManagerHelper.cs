// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComputingManagerHelper.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.AWS.Domain;
    using Naos.Deployment.ComputingManagement;
    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Scheduler;
    using Naos.Deployment.Tracking;
    using OBeautifulCode.Assertion.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Helper class to share methods across handlers.
    /// </summary>
    public static class ComputingManagerHelper
    {
        /// <summary>
        /// Creates a new computing manager from settings.
        /// </summary>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings for the computing infrastructure manager.</param>
        /// <returns>New computing manager.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Factory method is not suppossed to dispose...")]
        public static IManageComputingInfrastructure CreateComputingManager(DeploymentMessageHandlerSettings settings, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings)
        {
            new { settings }.AsArg().Must().NotBeNull();

            var credentialsToUse = new CredentialContainer
            {
                AccessKeyId = settings.AccessKey,
                SecretAccessKey = settings.SecretKey,
                CredentialType = CredentialType.Keys,
            };

            var computingManager =
                new ComputingInfrastructureManagerForAws(computingInfrastructureManagerSettings)
                    .InitializeCredentials(credentialsToUse);

            return computingManager;
        }

        /// <summary>
        /// Gets the system id from the instance name looking in the specified arcology.
        /// </summary>
        /// <param name="name">Name of the instance (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <param name="settings">Handler settings.</param>
        /// <returns>System id matching the specified name, throws if not found.</returns>
        private static async Task<string> GetSystemIdFromNameFromArcologyAsync(string name, DeploymentMessageHandlerSettings settings)
        {
            var tracker = InfrastructureTrackerFactory.Create(settings.InfrastructureTrackerConfiguration);
            var ret = await tracker.GetInstanceIdByNameAsync(settings.Environment, name);
            return ret;
        }

        /// <summary>
        /// Gets the system id from the instance name checking name tags on the provider's instance details.
        /// </summary>
        /// <param name="instanceName">Name of the instance to lookup.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings that contain details about how to use the computer infrastructure manager.</param>
        /// <param name="settings">Handler settings.</param>
        /// <param name="computingManager">Computing manager.</param>
        /// <returns>System id matching the specified name, throws if not found.</returns>
        private static async Task<string> GetSystemIdFromNameFromTagAsync(string instanceName, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings, DeploymentMessageHandlerSettings settings, IManageComputingInfrastructure computingManager)
        {
            var namer = new ComputingInfrastructureNamer(
                instanceName,
                settings.Environment,
                settings.ContainerSystemLocation);

            var fullInstanceName = namer.GetInstanceName();

            var providerInstances = await computingManager.GetActiveInstancesFromProviderAsync(settings.Environment);
            var instance =
                providerInstances.SingleOrDefault(
                    _ =>
                        {
                            var environmentTag = _.Tags.SingleOrDefault(tag => tag.Key == computingInfrastructureManagerSettings.EnvironmentTagKey);
                            var matches = !default(KeyValuePair<string, string>).Equals(environmentTag)
                                          && environmentTag.Value == settings.Environment && _.Name == fullInstanceName;
                            return matches;
                        });

            if (instance == null)
            {
                throw new ArgumentException(
                    "Could not find instance by name: " + fullInstanceName + " in environment: " + settings.Environment);
            }

            return instance.Id;
        }

        private static async Task<IReadOnlyCollection<string>> GetSystemIdsFromTagInArcologyAsync(IReadOnlyDictionary<string, string> tagsToMatch, TagMatchStrategy tagMatchStrategy, DeploymentMessageHandlerSettings settings)
        {
            var tracker = InfrastructureTrackerFactory.Create(settings.InfrastructureTrackerConfiguration);
            var allDescriptions = await tracker.GetAllInstanceDescriptionsAsync(settings.Environment);
            var ret = new List<string>();
            foreach (var description in allDescriptions)
            {
                if (IsTagMatch(description.Tags, tagsToMatch, tagMatchStrategy))
                {
                    ret.Add(description.Id);
                }
            }

            return ret;
        }

        private static async Task<IReadOnlyCollection<string>> GetSystemIdsFromTagInProviderAsync(
            IReadOnlyDictionary<string, string> tagsToMatch,
            TagMatchStrategy tagMatchStrategy,
            DeploymentMessageHandlerSettings settings,
            IManageComputingInfrastructure computingManager)
        {
            var providerInstances = await computingManager.GetActiveInstancesFromProviderAsync(settings.Environment);
            var ret = new List<string>();
            foreach (var instance in providerInstances)
            {
                if (IsTagMatch(instance.Tags, tagsToMatch, tagMatchStrategy))
                {
                    ret.Add(instance.Id);
                }
            }

            return ret;
        }

        /// <summary>
        /// Matches tags to a set using a strategy.
        /// </summary>
        /// <param name="tags">Tags to match against.</param>
        /// <param name="tagsToMatch">Tags to match.</param>
        /// <param name="tagMatchStrategy">Strategy for matching.</param>
        /// <returns>A value indicating whether or not there is a match.</returns>
        public static bool IsTagMatch(IReadOnlyDictionary<string, string> tags, IReadOnlyDictionary<string, string> tagsToMatch, TagMatchStrategy tagMatchStrategy)
        {
            Func<IReadOnlyCollection<KeyValuePair<string, string>>, Func<KeyValuePair<string, string>, bool>, bool> evaluateMatchCriteriaOnItemsMethod;

            switch (tagMatchStrategy)
            {
                case TagMatchStrategy.All:
                    evaluateMatchCriteriaOnItemsMethod = Enumerable.All;
                    break;
                case TagMatchStrategy.Any:
                    evaluateMatchCriteriaOnItemsMethod = Enumerable.Any;
                    break;
                default:
                    throw new NotSupportedException(Invariant($"Unsupported {nameof(TagMatchStrategy)}; {tagMatchStrategy}"));
            }

            var safeTagsToMatch = tagsToMatch ?? new Dictionary<string, string>();
            var safeTags = tags ?? new Dictionary<string, string>();
            return evaluateMatchCriteriaOnItemsMethod(safeTagsToMatch, _ => safeTags.Contains(_));
        }

        /// <summary>
        /// Gets a system ID using the specified targeter.
        /// </summary>
        /// <param name="instanceTargeter">Targeter to use.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings that contain details about how to use the computer infrastructure manager.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingManager">Computing infrastructure manager to perform operations.</param>
        /// <returns>System specific ID to use for operations.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeter", Justification = "Spelling/name is correct.")]
        public static async Task<IReadOnlyCollection<string>> GetSystemIdsFromTargeterAsync(
            InstanceTargeterBase instanceTargeter,
            ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings,
            DeploymentMessageHandlerSettings settings,
            IManageComputingInfrastructure computingManager)
        {
            IReadOnlyCollection<string> ret;
            if (instanceTargeter is InstanceTargeterSystemId asId)
            {
                ret = new[] { asId.InstanceId };
            }
            else if (instanceTargeter is InstanceTargeterTagMatch asTag)
            {
                var tags = asTag.Tags;
                if (tags.ContainsKey(computingInfrastructureManagerSettings.EnvironmentTagKey))
                {
                    var specifiedEnvironment = tags[computingInfrastructureManagerSettings.EnvironmentTagKey];
                    if (specifiedEnvironment != settings.Environment)
                    {
                        throw new NotSupportedException(Invariant($"Manipulating instances in other environments is not supported; specified environment: {specifiedEnvironment}, current environment {settings.Environment}"));
                    }
                }
                else
                {
                    tags = tags.Concat(new[] { new KeyValuePair<string, string>(computingInfrastructureManagerSettings.EnvironmentTagKey, settings.Environment), })
                               .ToDictionary(k => k.Key, v => v.Value);
                }

                switch (settings.InstanceLookupSource)
                {
                    case InstanceLookupSource.Provider:
                        ret = await GetSystemIdsFromTagInProviderAsync(
                                  tags,
                                  asTag.TagMatchStrategy,
                                  settings,
                                  computingManager);
                        break;
                    case InstanceLookupSource.Arcology:
                        ret = await GetSystemIdsFromTagInArcologyAsync(tags, asTag.TagMatchStrategy, settings);
                        break;
                    default:
                        throw new NotSupportedException(
                            "InstanceLookupSource not supported: " + settings.InstanceLookupSource);
                }
            }
            else if (instanceTargeter is InstanceTargeterNameLookup asNameLookup)
            {
                switch (settings.InstanceLookupSource)
                {
                    case InstanceLookupSource.Provider:
                        var systemIdFromProviderNameTag =
                            await
                            GetSystemIdFromNameFromTagAsync(
                                asNameLookup.Name,
                                computingInfrastructureManagerSettings,
                                settings,
                                computingManager);
                        ret = new[] { systemIdFromProviderNameTag };
                        break;
                    case InstanceLookupSource.Arcology:
                        var systemIdFromArcologyName = await GetSystemIdFromNameFromArcologyAsync(asNameLookup.Name, settings);
                        ret = new[] { systemIdFromArcologyName };
                        break;
                    default:
                        throw new NotSupportedException(
                            "InstanceLookupSource not supported: " + settings.InstanceLookupSource);
                }
            }
            else
            {
                throw new NotSupportedException("InstanceTargeter not supported; type: " + instanceTargeter.GetType().FullName);
            }

            return ret;
        }
    }
}
