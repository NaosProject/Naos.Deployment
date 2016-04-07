// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComputingManagerHelper.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.AWS.Contract;
    using Naos.Deployment.ComputingManagement;
    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Handler;
    using Naos.Deployment.Tracking;

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
        public static IManageComputingInfrastructure CreateComputingManager(DeploymentMessageHandlerSettings settings, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings)
        {
            var credentialsToUse = new CredentialContainer
            {
                AccessKeyId = settings.AccessKey,
                SecretAccessKey = settings.SecretKey,
                CredentialType = CredentialType.Keys
            };

            var computingManager =
                new ComputingInfrastructureManagerForAws(computingInfrastructureManagerSettings, new NullInfrastructureTracker())
                    .InitializeCredentials(credentialsToUse);

            return computingManager;
        }

        /// <summary>
        /// Gets the system id from the instance name looking in the specified arcology.
        /// </summary>
        /// <param name="instanceName">Name of the instance to lookup.</param>
        /// <param name="settings">Handler settings.</param>
        /// <returns>System id matching the specified name, throws if not found.</returns>
        private static async Task<string> GetSystemIdFromNameFromArcologyAsync(string instanceName, DeploymentMessageHandlerSettings settings)
        {
            var tracker = InfrastructureTrackerFactory.Create(settings.InfrastructureTrackerConfiguration);

            var ret = await tracker.GetInstanceIdByNameAsync(settings.Environment, instanceName);

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

            var providerInstances = await computingManager.GetActiveInstancesFromProviderAsync(settings.Environment, settings.SystemLocation);
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

        /// <summary>
        /// Gets a system ID using the specified targeter.
        /// </summary>
        /// <param name="instanceTargeter">Targeter to use.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings that contain details about how to use the computer infrastructure manager.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingManager">Computing infrastructure manager to perform operations.</param>
        /// <returns>System specific ID to use for operations.</returns>
        public static async Task<string> GetSystemIdFromTargeterAsync(
            InstanceTargeterBase instanceTargeter,
            ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings,
            DeploymentMessageHandlerSettings settings,
            IManageComputingInfrastructure computingManager)
        {
            string ret;
            var type = instanceTargeter.GetType();
            if (type == typeof(InstanceTargeterSystemId))
            {
                var asId = (InstanceTargeterSystemId)instanceTargeter;
                ret = asId.InstanceId;
            }
            else if (type == typeof(InstanceTargeterNameLookup))
            {
                var asTag = (InstanceTargeterNameLookup)instanceTargeter;
                switch (settings.InstanceNameLookupSource)
                {
                    case InstanceNameLookupSource.ProviderTag:
                        ret =
                            await
                            GetSystemIdFromNameFromTagAsync(
                                asTag.InstanceName,
                                computingInfrastructureManagerSettings,
                                settings,
                                computingManager);
                        break;
                    case InstanceNameLookupSource.Arcology:
                        ret = await GetSystemIdFromNameFromArcologyAsync(asTag.InstanceName, settings);
                        break;
                    default:
                        throw new NotSupportedException(
                            "InstanceNameLookupSource not supported: " + settings.InstanceNameLookupSource);
                }
            }
            else
            {
                throw new NotSupportedException("InstanceTargeter not supported; type: " + type.FullName);
            }

            return ret;
        }
    }
}
