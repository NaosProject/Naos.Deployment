// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StartInstanceMessage.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using Naos.MessageBus.DataContract;

    /// <summary>
    /// Message to be processed and turn off an instance specified.
    /// </summary>
    public class StartInstanceMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the ID (per the cloud provider) of the instance to start.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to block until the instance is running.
        /// </summary>
        public bool WaitUntilOn { get; set; }

        /// <summary>
        /// Gets or sets the name of the instance
        /// </summary>
        public string InstanceName { get; set; }
    }
}
