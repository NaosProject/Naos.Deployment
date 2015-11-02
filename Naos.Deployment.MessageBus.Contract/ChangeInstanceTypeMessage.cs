// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChangeInstanceTypeMessage.cs" company="Naos">
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
    public class ChangeInstanceTypeMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the ID (per the cloud provider) of the instance to change the type of.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the new instance type to use for the instance.
        /// </summary>
        public InstanceType NewInstanceType { get; set; }

        /// <summary>
        /// Gets or sets the name of the instance
        /// </summary>
        public string InstanceName { get; set; }
    }
}
