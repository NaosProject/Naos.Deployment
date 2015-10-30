// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopInstanceMessage.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using Naos.MessageBus.DataContract;

    /// <summary>
    /// Message to be processed and turn off an instance specified.
    /// </summary>
    public class StopInstanceMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the ID (per the cloud provider) of the instance to stop.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the location (per the cloud provider) of the instance to stop.
        /// </summary>
        public string InstanceLocation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to block until the instance is stopped.
        /// </summary>
        public bool WaitUntilOff { get; set; }
    }
}
