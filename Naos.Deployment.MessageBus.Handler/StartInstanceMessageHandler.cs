// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StartInstanceMessageHandler.cs" company="Naos">
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
    /// Handler for start instance messages.
    /// </summary>
    public class StartInstanceMessageHandler : IHandleMessages<StartInstanceMessage>
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(StartInstanceMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var cloudInfrastructureManagerSettings = Settings.Get<CloudInfrastructureManagerSettings>();
            await Task.Run(() => this.Handle(message, settings, cloudInfrastructureManagerSettings));
        }

        /// <summary>
        /// Handle a start instance message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="cloudInfrastructureManagerSettings">Settings for the cloud infrastructure manager.</param>
        public void Handle(StartInstanceMessage message, DeploymentMessageHandlerSettings settings, CloudInfrastructureManagerSettings cloudInfrastructureManagerSettings)
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

            cloudManager.TurnOnInstance(message.InstanceId, message.InstanceLocation, message.WaitUntilOn);
        }
    }
}
