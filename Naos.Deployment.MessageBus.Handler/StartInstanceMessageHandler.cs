// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StartInstanceMessageHandler.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
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
    using Naos.Deployment.MessageBus.Scheduler;
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Handler for start instance messages.
    /// </summary>
    public class StartInstanceMessageHandler : MessageHandlerBase<StartInstanceMessage>, IShareInstanceTargeters
    {
        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(StartInstanceMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var computingInfrastructureManagerSettings = Settings.Get<ComputingInfrastructureManagerSettings>();
            await this.HandleAsync(message, settings, computingInfrastructureManagerSettings);
        }

        /// <summary>
        /// Handle a start instance message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings for the computing infrastructure manager.</param>
        /// <returns>Task for async execution.</returns>
        public async Task HandleAsync(StartInstanceMessage message, DeploymentMessageHandlerSettings settings, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings)
        {
            if (message == null)
            {
                throw new ArgumentException("Cannot have a null message.");
            }

            if (message.InstanceTargeters == null || message.InstanceTargeters.Length == 0)
            {
                throw new ArgumentException("Must specify at least one instance targeter to use for specifying an instance.");
            }

            var tasks =
                message.InstanceTargeters.Select(
                    (instanceTargeter) =>
                    Task.Run(
                        () => ParallelOperationAsync(instanceTargeter, computingInfrastructureManagerSettings, settings, message.WaitUntilOn)))
                    .ToArray();

            await Task.WhenAll(tasks);

            this.InstanceTargeters = message.InstanceTargeters;
        }

        private static async Task ParallelOperationAsync(InstanceTargeterBase instanceTargeter, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings, DeploymentMessageHandlerSettings settings, bool waitUntilOn)
        {
            using (var computingManager = ComputingManagerHelper.CreateComputingManager(settings, computingInfrastructureManagerSettings))
            {
                var systemIds =
                    await
                        ComputingManagerHelper.GetSystemIdsFromTargeterAsync(
                            instanceTargeter,
                            computingInfrastructureManagerSettings,
                            settings,
                            computingManager);

                foreach (var systemId in systemIds)
                {
                    if (string.IsNullOrWhiteSpace(systemId))
                    {
                        throw new ArgumentException(Invariant($"Could not find a {nameof(systemId)} for targeter: {instanceTargeter}."));
                    }

                    Log.Write(
                        () => new
                                  {
                                      Info = "Starting Instance",
                                      InstanceTargeterJson = LoggingHelper.SerializeToString(instanceTargeter),
                                      SystemId = systemId,
                                  });

                    await computingManager.TurnOnInstanceAsync(systemId, settings.SystemLocation, waitUntilOn, settings.MaxRebootsOnFailedStatusCheck);
                }
            }
        }
    }
}