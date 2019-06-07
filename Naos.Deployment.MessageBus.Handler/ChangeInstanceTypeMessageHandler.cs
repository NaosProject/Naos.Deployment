// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChangeInstanceTypeMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;
    using Naos.Configuration.Domain;
    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Scheduler;
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Handler for stop instance messages.
    /// </summary>
    public class ChangeInstanceTypeMessageHandler : MessageHandlerBase<ChangeInstanceTypeMessage>, IShareInstanceTargeters
    {
        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(ChangeInstanceTypeMessage message)
        {
            var settings = Config.Get<DeploymentMessageHandlerSettings>(typeof(NaosDeploymentMessageBusJsonConfiguration));
            var computingInfrastructureManagerSettings = Config.Get<ComputingInfrastructureManagerSettings>(typeof(NaosDeploymentMessageBusJsonConfiguration));
            await this.HandleAsync(message, settings, computingInfrastructureManagerSettings);
        }

        /// <summary>
        /// Handle a change instance type message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings for the computing infrastructure manager.</param>
        /// <returns>Task for async execution.</returns>
        public async Task HandleAsync(ChangeInstanceTypeMessage message, DeploymentMessageHandlerSettings settings, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings)
        {
            if (message.NewInstanceType == null)
            {
                throw new ArgumentException("Must specify a new instance type to change instance to.");
            }

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
                    instanceTargeter =>
                    Task.Run(
                        () => ParallelOperationAsync(computingInfrastructureManagerSettings, instanceTargeter, settings, message.NewInstanceType)))
                    .ToArray();

            await Task.WhenAll(tasks);

            this.InstanceTargeters = message.InstanceTargeters;
        }

        private static async Task ParallelOperationAsync(ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings, InstanceTargeterBase instanceTargeter, DeploymentMessageHandlerSettings settings, InstanceType newInstanceType)
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
                                      Info = "Changing Instance Type",
                                      InstanceTargeterJson = LoggingHelper.SerializeToString(instanceTargeter),
                                      NewInstanceType = LoggingHelper.SerializeToString(newInstanceType),
                                      SystemId = systemId,
                                  });

                    await computingManager.ChangeInstanceTypeAsync(systemId, settings.SystemLocation, newInstanceType);
                }
            }
        }
    }
}