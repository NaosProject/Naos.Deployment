// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloudManagerHelper.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using System;
    using System.Linq;

    using Naos.AWS.Contract;
    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;

    /// <summary>
    /// Helper class to share methods across handlers.
    /// </summary>
    public static class CloudManagerHelper
    {
        /// <summary>
        /// Creates a new cloud manager from settings.
        /// </summary>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="cloudInfrastructureManagerSettings">Settings for the cloud infrastructure manager.</param>
        /// <returns>New cloud manager.</returns>
        public static IManageCloudInfrastructure CreateCloudManager(DeploymentMessageHandlerSettings settings, CloudInfrastructureManagerSettings cloudInfrastructureManagerSettings)
        {
            var credentialsToUse = new CredentialContainer
            {
                AccessKeyId = settings.AccessKey,
                SecretAccessKey = settings.SecretKey,
                CredentialType = CredentialType.Keys
            };

            var cloudManager =
                new CloudInfrastructureManager(cloudInfrastructureManagerSettings, new NullInfrastructureTracker())
                    .InitializeCredentials(credentialsToUse);

            return cloudManager;
        }

        /// <summary>
        /// Gets the system id from the instance name.
        /// </summary>
        /// <param name="instanceName">Name of the instance to lookup.</param>
        /// <param name="settings">Handler settings.</param>
        /// <param name="cloudManager">Cloud manager.</param>
        /// <returns>System id matching the specified name, throws if not found.</returns>
        public static string GetSystemIdFromName(
            string instanceName,
            DeploymentMessageHandlerSettings settings,
            IManageCloudInfrastructure cloudManager)
        {
            var namer = new CloudInfrastructureNamer(
                instanceName,
                settings.Environment,
                settings.ContainerSystemLocation);

            var fullInstanceName = namer.GetInstanceName();

            var cloudInstances = cloudManager.GetInstancesFromCloud(settings.Environment, settings.SystemLocation);
            var instance =
                cloudInstances.SingleOrDefault(
                    _ => _.Tags[Constants.EnvironmentTagKey] == settings.Environment && _.Name == fullInstanceName);

            if (instance == null)
            {
                throw new ArgumentException(
                    "Could not find instance by name: " + fullInstanceName + " in environment: " + settings.Environment);
            }

            return instance.Id;
        }
    }
}
