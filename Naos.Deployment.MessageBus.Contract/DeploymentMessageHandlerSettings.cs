// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentMessageHandlerSettings.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
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
    }
}
