// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StartInstanceMessageHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Its.Configuration;
    using Its.Log.Instrumentation;

    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;
    using Naos.Deployment.MessageBus.Contract;
    using Naos.MessageBus.HandlingContract;

    /// <summary>
    /// Handler for start instance messages.
    /// </summary>
    public class StartInstanceMessageHandler : IHandleMessages<StartInstanceMessage>, IShareInstanceTargeter
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(StartInstanceMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var cloudInfrastructureManagerSettings = Settings.Get<CloudInfrastructureManagerSettings>();
            await this.Handle(message, settings, cloudInfrastructureManagerSettings);
        }

        /// <summary>
        /// Handle a start instance message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="cloudInfrastructureManagerSettings">Settings for the cloud infrastructure manager.</param>
        /// <returns>Task for async execution.</returns>
        public async Task Handle(StartInstanceMessage message, DeploymentMessageHandlerSettings settings, CloudInfrastructureManagerSettings cloudInfrastructureManagerSettings)
        {
            if (message == null)
            {
                throw new ArgumentException("Cannot have a null message.");
            }

            if (message.InstanceTargeters == null || message.InstanceTargeters.Length == 0)
            {
                throw new ArgumentException("Must specify at least one instance targeter to use for specifying an instance.");
            }

            var cloudManager = CloudManagerHelper.CreateCloudManager(settings, cloudInfrastructureManagerSettings);

            var tasks =
                message.InstanceTargeters.Select(
                    (instanceTargeter) =>
                    Task.Run(
                        () => OperationToParallelize(instanceTargeter, settings, cloudManager, message.WaitUntilOn)))
                    .ToArray();

            await Task.WhenAll(tasks);

            this.InstanceTargeters = message.InstanceTargeters;
        }

        private static async Task OperationToParallelize(
            InstanceTargeterBase instanceTargeter,
            DeploymentMessageHandlerSettings settings,
            IManageCloudInfrastructure cloudManager,
            bool waitUntilOn)
        {
            var systemId =
                await CloudManagerHelper.GetSystemIdFromTargeterAsync(instanceTargeter, settings, cloudManager);

            Log.Write(
                () => new { Info = "Starting Instance", InstanceTargeterJson = Serializer.Serialize(instanceTargeter), SystemId = systemId });
            await cloudManager.TurnOnInstanceAsync(systemId, settings.SystemLocation, waitUntilOn);
        }
    }
}