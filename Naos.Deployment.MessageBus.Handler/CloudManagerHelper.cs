// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloudManagerHelper.cs" company="Naos">
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
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.DataContract;

    /// <summary>
    /// Helper class to share methods across handlers.
    /// </summary>
    public static class CloudManagerHelper
    {
        /// <summary>
        /// Creates a new cloud manager from settings.
        /// </summary>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings for the cloud infrastructure manager.</param>
        /// <returns>New cloud manager.</returns>
        public static IManageComputingInfrastructure CreateComputingManager(DeploymentMessageHandlerSettings settings, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings)
        {
            var credentialsToUse = new CredentialContainer
            {
                AccessKeyId = settings.AccessKey,
                SecretAccessKey = settings.SecretKey,
                CredentialType = CredentialType.Keys
            };

            var cloudManager =
                new ComputingInfrastructureManagerForAws(computingInfrastructureManagerSettings, new NullInfrastructureTracker())
                    .InitializeCredentials(credentialsToUse);

            return cloudManager;
        }

        /// <summary>
        /// Gets the system id from the instance name.
        /// </summary>
        /// <param name="instanceName">Name of the instance to lookup.</param>
        /// <param name="settings">Handler settings.</param>
        /// <param name="computingManager">Cloud manager.</param>
        /// <returns>System id matching the specified name, throws if not found.</returns>
        public static async Task<string> GetSystemIdFromNameAsync(
            string instanceName,
            DeploymentMessageHandlerSettings settings,
            IManageComputingInfrastructure computingManager)
        {
            var namer = new ComputingInfrastructureNamer(
                instanceName,
                settings.Environment,
                settings.ContainerSystemLocation);

            var fullInstanceName = namer.GetInstanceName();

            var cloudInstances = await computingManager.GetActiveInstancesFromCloudAsync(settings.Environment, settings.SystemLocation);
            var instance =
                cloudInstances.SingleOrDefault(
                    _ =>
                        {
                            var environmentTag = _.Tags.SingleOrDefault(tag => tag.Key == Constants.EnvironmentTagKey);
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
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingManager">Cloud infrastructure manager to perform operations.</param>
        /// <returns>System specific ID to use for operations.</returns>
        public static async Task<string> GetSystemIdFromTargeterAsync(InstanceTargeterBase instanceTargeter, DeploymentMessageHandlerSettings settings, IManageComputingInfrastructure computingManager)
        {
            string ret;
            var type = instanceTargeter.GetType();
            if (type == typeof(InstanceTargeterSystemId))
            {
                var asId = (InstanceTargeterSystemId)instanceTargeter;
                ret = asId.InstanceId;
            }
            else if (type == typeof(InstanceTargeterNameLookupByCloudTag))
            {
                var asTag = (InstanceTargeterNameLookupByCloudTag)instanceTargeter;
                ret = await GetSystemIdFromNameAsync(asTag.InstanceNameInTag, settings, computingManager);
            }
            else
            {
                throw new NotSupportedException("InstanceTargeter not supported; type: " + type.FullName);
            }

            return ret;
        }
    }
}
