// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentMessageHandlerSettings.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using Naos.Deployment.MessageBus.Contract;
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
        /// Gets or sets the source to use for looking up an instance by name.
        /// </summary>
        public InstanceNameLookupSource InstanceNameLookupSource { get; set; }

        /// <summary>
        /// Gets or sets the infrastructure tracker configuration.
        /// </summary>
        public InfrastructureTrackerConfigurationBase InfrastructureTrackerConfiguration { get; set; }
    }
}
