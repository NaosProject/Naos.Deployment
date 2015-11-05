// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareInstanceTargeterMessage.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using Naos.Deployment.Contract;
    using Naos.MessageBus.DataContract;

    /// <summary>
    /// Message to share an instance targeter.
    /// </summary>
    public class ShareInstanceTargeterMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the instance targeter to share with other messages in the sequence.
        /// </summary>
        public InstanceTargeterBase InstanceTargeterToShare { get; set; }
    }
}
