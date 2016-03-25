// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopInstanceMessageHandler.cs" company="Naos">
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

    using Naos.Deployment.Domain;
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
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(StopInstanceMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var computingInfrastructureManagerSettings = Settings.Get<ComputingInfrastructureManagerSettings>();
            await this.Handle(message, settings, computingInfrastructureManagerSettings);
        }

        /// <summary>
        /// Handle a stop instance message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings for the computing manager.</param>
        /// <returns>Task for async execution.</returns>
        public async Task Handle(StopInstanceMessage message, DeploymentMessageHandlerSettings settings, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings)
        {
            if (message == null)
            {
                throw new ArgumentException("Cannot have a null message.");
            }

            if (message.InstanceTargeters == null || message.InstanceTargeters.Length  == 0)
            {
                throw new ArgumentException("Must specify at least one instance targeter to use for specifying an instance.");
            }

            var tasks =
                message.InstanceTargeters.Select(
                    (instanceTargeter) =>
                    Task.Run(
                        () => OperationToParallelize(instanceTargeter, computingInfrastructureManagerSettings, settings, message.WaitUntilOff)))
                    .ToArray();

            await Task.WhenAll(tasks);

            this.InstanceTargeters = message.InstanceTargeters;
        }

        private static async Task OperationToParallelize(InstanceTargeterBase instanceTargeter, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings, DeploymentMessageHandlerSettings settings, bool waitUntilOff)
        {
            var computingManager = ComputingManagerHelper.CreateComputingManager(settings, computingInfrastructureManagerSettings);

            var systemId =
                await ComputingManagerHelper.GetSystemIdFromTargeterAsync(instanceTargeter, computingInfrastructureManagerSettings, settings, computingManager);

            Log.Write(
                () => new { Info = "Stopping Instance", InstanceTargeterJson = Serializer.Serialize(instanceTargeter), SystemId = systemId });
            await computingManager.TurnOffInstanceAsync(systemId, settings.SystemLocation, waitUntilOff);
        }
    }
}
