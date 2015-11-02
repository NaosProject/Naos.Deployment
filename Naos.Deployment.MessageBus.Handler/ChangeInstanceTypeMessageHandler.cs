// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChangeInstanceTypeMessageHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Its.Configuration;

    using Naos.AWS.Contract;
    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;
    using Naos.MessageBus.HandlingContract;

    /// <summary>
    /// Handler for stop instance messages.
    /// </summary>
    public class ChangeInstanceTypeMessageHandler : IHandleMessages<ChangeInstanceTypeMessage>
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(ChangeInstanceTypeMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var cloudInfrastructureManagerSettings = Settings.Get<CloudInfrastructureManagerSettings>();
            await Task.Run(() => this.Handle(message, settings, cloudInfrastructureManagerSettings));
        }

        /// <summary>
        /// Handle a change instance type message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="cloudInfrastructureManagerSettings">Settings for the cloud infrastructure manager.</param>
        public void Handle(ChangeInstanceTypeMessage message, DeploymentMessageHandlerSettings settings, CloudInfrastructureManagerSettings cloudInfrastructureManagerSettings)
        {
            if (message.NewInstanceType == null)
            {
                throw new ArgumentException("Must specify a new instance type to change instance to.");
            }

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

            cloudManager.ChangeInstanceType(systemId, settings.SystemLocation, message.NewInstanceType);
        }
    }
}
