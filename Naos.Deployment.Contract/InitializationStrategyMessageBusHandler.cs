// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyMessageBusHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    using Naos.MessageBus.DataContract;

    /// <summary>
    /// Custom extension of the InitializationStrategyBase to accommodate message bus handler deployments.
    /// </summary>
    public class InitializationStrategyMessageBusHandler : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the channels to monitor on the message bus system.
        /// </summary>
        public ICollection<Channel> ChannelsToMonitor { get; set; }
    }
}
