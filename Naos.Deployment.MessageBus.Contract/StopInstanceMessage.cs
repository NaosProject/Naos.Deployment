// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopInstanceMessage.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using System.Collections.Generic;

    using Naos.Deployment.Contract;
    using Naos.MessageBus.DataContract;

    /// <summary>
    /// Message to be processed and turn off an instance specified.
    /// </summary>
    public class StopInstanceMessage : IMessage, IShareInstanceTargeter
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public IList<InstanceTargeterBase> InstanceTargeters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to block until the instance is stopped.
        /// </summary>
        public bool WaitUntilOff { get; set; }
    }
}
