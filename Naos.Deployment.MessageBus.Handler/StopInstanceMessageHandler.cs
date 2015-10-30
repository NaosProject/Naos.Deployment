// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopInstanceMessageHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using System.Threading.Tasks;

    using Its.Configuration;

    using Naos.AWS.Contract;
    using Naos.Deployment.CloudManagement;
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
            await Task.Run(() => this.Handle(message, settings));
        }

        /// <summary>
        /// Handle a stop instance message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        public void Handle(StopInstanceMessage message, DeploymentMessageHandlerSettings settings)
        {
            var credentialsToUse = new CredentialContainer
            {
                AccessKeyId = settings.AccessKey,
                SecretAccessKey = settings.SecretKey,
                CredentialType = CredentialType.Keys
            };

            // the absense of real settings will reduce what the manager can do, will move this out to a smaller manager later.
            var managerSettings = new CloudInfrastructureManagerSettings();
            var cloudManager =
                new CloudInfrastructureManager(managerSettings, new NullInfrastructureTracker()).InitializeCredentials(
                    credentialsToUse);

            cloudManager.TurnOffInstance(message.InstanceId, message.InstanceLocation, message.WaitUntilOff);
        }
    }
}
