// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentMessageHandlerSettings.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using Naos.Deployment.MessageBus.Scheduler;
    using Naos.Deployment.Tracking;

    /// <summary>
    /// Settings for the handler.
    /// </summary>
    public class DeploymentMessageHandlerSettings
    {
        /// <summary>
        /// Gets or sets the access key for computing platform operations.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the secret key for computing platform operations.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Gets or sets the environment the handler is servicing.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the system location of the environment the handler is servicing.
        /// </summary>
        public string SystemLocation { get; set; }

        /// <summary>
        /// Gets or sets the system container location of the environment.
        /// </summary>
        public string ContainerSystemLocation { get; set; }

        /// <summary>
        /// Gets or sets the max reboots to attempt on a failed status check start to get it to migrate and work.
        /// </summary>
        public int MaxRebootsOnFailedStatusCheck { get; set; }

        /// <summary>
        /// Gets or sets the source to use for looking up an instance.
        /// </summary>
        public InstanceLookupSource InstanceLookupSource { get; set; }

        /// <summary>
        /// Gets or sets the infrastructure tracker configuration.
        /// </summary>
        public InfrastructureTrackerConfigurationBase InfrastructureTrackerConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the delay on stopping an instance (can be used for remaining logs to be shipped or other reasons).
        /// </summary>
        public TimeSpan StopInstanceDelay { get; set; }

        /// <summary>
        /// Gets or sets the channel name to use when rescheduling messages.
        /// </summary>
        public string ReschedulingChannelName { get; set; }
    }
}
