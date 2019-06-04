// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopInstanceWithDelayMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Its.Configuration;

    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Scheduler;
    using Naos.MessageBus.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Handler for stop instance messages.
    /// </summary>
    public class StopInstanceWithDelayMessageHandler : MessageHandlerBase<StopInstanceWithDelayMessage>, IShareInstanceTargeters
    {
        private readonly object postOfficeLock = new object();

        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(StopInstanceWithDelayMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var computingInfrastructureManagerSettings = Settings.Get<ComputingInfrastructureManagerSettings>();
            await this.HandleAsync(message, settings, computingInfrastructureManagerSettings);
        }

        /// <summary>
        /// Handle a stop instance message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings for the computing manager.</param>
        /// <returns>Task for async execution.</returns>
        public async Task HandleAsync(StopInstanceWithDelayMessage message, DeploymentMessageHandlerSettings settings, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings)
        {
            if (message == null)
            {
                throw new ArgumentException("Cannot have a null message.");
            }

            if (message.InstanceTargeters == null || message.InstanceTargeters.Length == 0)
            {
                throw new ArgumentException("Must specify at least one instance targeter to use for specifying an instance.");
            }

            var utcNow = DateTime.UtcNow;
            if (message.MinimumDateTimeInUtcBeforeStop > utcNow)
            {
                var timeToSleep = message.MinimumDateTimeInUtcBeforeStop.Subtract(utcNow);
                Thread.Sleep(timeToSleep);
            }

            var tasks =
                message.InstanceTargeters.Select(
                        instanceTargeter =>
                            Task.Run(
                                () => InstanceOperationHelper.ParallelOperationForStopInstanceAsync(
                                    instanceTargeter,
                                    computingInfrastructureManagerSettings,
                                    settings,
                                    this.postOfficeLock,
                                    this.PostOffice)))
                    .ToArray();

            await Task.WhenAll(tasks);

            this.InstanceTargeters = message.InstanceTargeters;
        }
    }
}
