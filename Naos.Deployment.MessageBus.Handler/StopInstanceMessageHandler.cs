// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopInstanceMessageHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using System;
    using System.Threading.Tasks;

    using Its.Configuration;

    using Naos.Deployment.Contract;
    using Naos.MessageBus.HandlingContract;

    /// <summary>
    /// Handler for stop instance messages.
    /// </summary>
    public class StopInstanceMessageHandler : IHandleMessages<StopInstanceMessage>
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(StopInstanceMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var cloudInfrastructureManagerSettings = Settings.Get<CloudInfrastructureManagerSettings>();
            await Task.Run(() => this.Handle(message, settings, cloudInfrastructureManagerSettings));
        }

        /// <summary>
        /// Handle a stop instance message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="cloudInfrastructureManagerSettings">Settings for the cloud infrastructure manager.</param>
        public void Handle(StopInstanceMessage message, DeploymentMessageHandlerSettings settings, CloudInfrastructureManagerSettings cloudInfrastructureManagerSettings)
        {
            if (message == null)
            {
                throw new ArgumentException("Cannot have a null message.");
            }

            if (string.IsNullOrEmpty(message.InstanceId) && string.IsNullOrEmpty(message.InstanceName))
            {
                throw new ArgumentException("Must specify EITHER the instance id or name to lookup id by.");
            }

            var cloudManager = CloudManagerHelper.CreateCloudManager(settings, cloudInfrastructureManagerSettings);
            var systemId = message.InstanceId;
            if (string.IsNullOrEmpty(systemId))
            {
                systemId = CloudManagerHelper.GetSystemIdFromName(message.InstanceName, settings, cloudManager);
            }

            cloudManager.TurnOffInstance(systemId, settings.SystemLocation, message.WaitUntilOff);
        }
    }
}
