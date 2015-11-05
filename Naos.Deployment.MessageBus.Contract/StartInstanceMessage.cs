// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StartInstanceMessage.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using Naos.Deployment.Contract;
    using Naos.MessageBus.DataContract;

    /// <summary>
    /// Message to be processed and turn off an instance specified.
    /// </summary>
    public class StartInstanceMessage : IMessage, IShareInstanceTargeter
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public InstanceTargeterBase InstanceTargeter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to block until the instance is running.
        /// </summary>
        public bool WaitUntilOn { get; set; }
    }
}
