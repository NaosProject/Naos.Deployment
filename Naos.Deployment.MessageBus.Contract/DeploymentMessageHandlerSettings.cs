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
        /// Gets or sets the access key for cloud operations.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the secret key for cloud operations.
        /// </summary>
        public string SecretKey { get; set; }
    }
}
