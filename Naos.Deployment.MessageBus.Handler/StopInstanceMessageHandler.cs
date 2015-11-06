// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopInstanceMessageHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Its.Configuration;
    using Its.Log.Instrumentation;

    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;
    using Naos.Deployment.MessageBus.Contract;
    using Naos.MessageBus.HandlingContract;

    /// <summary>
    /// Handler for stop instance messages.
    /// </summary>
    public class StopInstanceMessageHandler : IHandleMessages<StopInstanceMessage>, IShareInstanceTargeter
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public IList<InstanceTargeterBase> InstanceTargeters { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(StopInstanceMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var cloudInfrastructureManagerSettings = Settings.Get<CloudInfrastructureManagerSettings>();
            await this.Handle(message, settings, cloudInfrastructureManagerSettings);
        }

        /// <summary>
        /// Handle a stop instance message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="cloudInfrastructureManagerSettings">Settings for the cloud infrastructure manager.</param>
        /// <returns>Task for async execution.</returns>
        public async Task Handle(StopInstanceMessage message, DeploymentMessageHandlerSettings settings, CloudInfrastructureManagerSettings cloudInfrastructureManagerSettings)
        {
            if (message == null)
            {
                throw new ArgumentException("Cannot have a null message.");
            }

            if (message.InstanceTargeters == null || message.InstanceTargeters.Count  == 0)
            {
                throw new ArgumentException("Must specify at least one instance targeter to use for specifying an instance.");
            }

            var cloudManager = CloudManagerHelper.CreateCloudManager(settings, cloudInfrastructureManagerSettings);

            var tasks =
                message.InstanceTargeters.Select(
                    (instanceTargeter) =>
                    Task.Run(
                        () => OperationToParallelize(instanceTargeter, settings, cloudManager, message.WaitUntilOff)))
                    .ToArray();

            await Task.WhenAll(tasks);

            this.InstanceTargeters = message.InstanceTargeters;
        }

        private static async Task OperationToParallelize(
            InstanceTargeterBase instanceTargeter,
            DeploymentMessageHandlerSettings settings,
            IManageCloudInfrastructure cloudManager,
            bool waitUntilOff)
        {
            var systemId =
                await CloudManagerHelper.GetSystemIdFromTargeterAsync(instanceTargeter, settings, cloudManager);

            Log.Write(
                () => new { Info = "Stopping Instance", InstanceTargeterJson = Serializer.Serialize(instanceTargeter), SystemId = systemId });
            await cloudManager.TurnOffInstanceAsync(systemId, settings.SystemLocation, waitUntilOff);
        }
    }
}
