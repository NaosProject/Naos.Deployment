// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StartInstanceMessage.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using Naos.Deployment.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message to be processed and turn off an instance specified.
    /// </summary>
    public class StartInstanceMessage : IMessage, IShareInstanceTargeters
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to block until the instance is running.
        /// </summary>
        public bool WaitUntilOn { get; set; }
    }
}
