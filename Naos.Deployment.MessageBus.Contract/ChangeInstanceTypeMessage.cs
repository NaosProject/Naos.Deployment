// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChangeInstanceTypeMessage.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using Naos.Deployment.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message to be processed and turn off an instance specified.
    /// </summary>
    public class ChangeInstanceTypeMessage : IMessage, IShareInstanceTargeter
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <summary>
        /// Gets or sets the new instance type to use for the instance.
        /// </summary>
        public InstanceType NewInstanceType { get; set; }
    }
}
