// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceOperationHelper.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Its.Log.Instrumentation;

    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Scheduler;
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.Domain;
    using OBeautifulCode.Representation.System;
    using OBeautifulCode.Type;
    using static System.FormattableString;

    /// <summary>
    /// Helper class to run parallel operations from handlers.
    /// </summary>
    public static class InstanceOperationHelper
    {
        /// <summary>
        /// Parallelizable helper method to start an instance.
        /// </summary>
        /// <param name="instanceTargeter">Instance targeter to use.</param>
        /// <param name="computingInfrastructureManagerSettings">Computing infrastructure manager settings to get enough context to operate with the computing platform.</param>
        /// <param name="settings">Settings for the the deployment handlers.</param>
        /// <param name="waitUntilOn">A value indicating to block until the server has started.</param>
        /// <returns>Task for async.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeter", Justification = "Spelling/name is correct.")]
        public static async Task ParallelOperationForStartInstanceAsync(
            InstanceTargeterBase instanceTargeter,
            ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings,
            DeploymentMessageHandlerSettings settings,
            bool waitUntilOn)
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

        /// <summary>
        /// Parallelizable helper method to stop an instance or schedule it for delayed stopping.
        /// </summary>
        /// <param name="instanceTargeter">Instance targeter to use.</param>
        /// <param name="computingInfrastructureManagerSettings">Computing infrastructure manager settings to get enough context to operate with the computing platform.</param>
        /// <param name="settings">Settings for the the deployment handlers.</param>
        /// <param name="postOfficeLock">Locking object to use with the provided <paramref name="postOffice" />.</param>
        /// <param name="postOffice">Implementation of <see cref="IPostOffice" /> to use for rescheduling a delayed shutdown.</param>
        /// <param name="stopInstanceDelay">Delay for stopping the instance (default is none).</param>
        /// <param name="waitUntilOff">A value indicating to block until the server has shutdown.</param>
        /// <returns>Task for async.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeter", Justification = "Spelling/name is correct.")]
        public static async Task ParallelOperationForStopInstanceAsync(
            InstanceTargeterBase instanceTargeter,
            ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings,
            DeploymentMessageHandlerSettings settings,
            object postOfficeLock,
            IPostOffice postOffice,
            TimeSpan stopInstanceDelay = default(TimeSpan),
            bool waitUntilOff = true)
        {
            using (var computingManager = ComputingManagerHelper.CreateComputingManager(settings, computingInfrastructureManagerSettings))
            {
                var systemIdsAsReadOnlyCollection =
                    await
                        ComputingManagerHelper.GetSystemIdsFromTargeterAsync(
                            instanceTargeter,
                            computingInfrastructureManagerSettings,
                            settings,
                            computingManager);

                var systemIds = systemIdsAsReadOnlyCollection.ToList();

                // confirm we don't have null values
                systemIds.ForEach(systemId =>
                {
                    if (string.IsNullOrWhiteSpace(systemId))
                    {
                        throw new ArgumentException(
                            Invariant($"Could not find a {nameof(systemId)} for targeter: {instanceTargeter}."));
                    }
                });

                // these are the same value but here for clarity...
                if (stopInstanceDelay == TimeSpan.Zero || stopInstanceDelay == default(TimeSpan))
                {
                    systemIds.ForEach(async systemId =>
                    {
                        Log.Write(
                            () => new
                            {
                                Info = "Stopping Instance",
                                InstanceTargeterJson = LoggingHelper.SerializeToString(instanceTargeter),
                                SystemId = systemId,
                            });

                        await computingManager.TurnOffInstanceAsync(
                            systemId,
                            settings.SystemLocation,
                            waitUntilOff);
                    });
                }
                else
                {
                    var minimumDateTimeInUtcBeforeStop = DateTime.UtcNow.Add(stopInstanceDelay);
                    var systemIdsToStopWithDelayAsSingleString = string.Join(",", systemIds);

                    Log.Write(
                        () => new
                        {
                            Info = "Stopping Instance around (UTC) " + minimumDateTimeInUtcBeforeStop,
                            InstanceTargeterJson = LoggingHelper.SerializeToString(instanceTargeter),
                            SystemIds = systemIdsToStopWithDelayAsSingleString,
                        });

                    var instanceTargeters = systemIds.Select(_ => (InstanceTargeterBase)new InstanceTargeterSystemId(_)).ToArray();

                    var stopInstanceWithDelayMessage = new StopInstanceWithDelayMessage
                    {
                        Description =
                            Invariant(
                                $"Stopping instances ({systemIdsToStopWithDelayAsSingleString}) @ {minimumDateTimeInUtcBeforeStop}"),
                        InstanceTargeters = instanceTargeters,
                        MinimumDateTimeInUtcBeforeStop = minimumDateTimeInUtcBeforeStop,
                    };

                    lock (postOfficeLock)
                    {
                        var addressedMessage = stopInstanceWithDelayMessage.ToAddressedMessage(
                            new SimpleChannel(settings.ReschedulingChannelName),
                            typeof(NaosDeploymentMessageBusJsonConfiguration).ToRepresentation());

                        postOffice.Send(new MessageSequence
                        {
                            Id = Guid.NewGuid(),
                            AddressedMessages = new[] { addressedMessage },
                        });
                    }
                }
            }
        }
    }
}
