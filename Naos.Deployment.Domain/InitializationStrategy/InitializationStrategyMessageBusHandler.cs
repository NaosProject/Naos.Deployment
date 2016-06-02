// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyMessageBusHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.MessageBus.Domain;

    /// <summary>
    /// Custom extension of the InitializationStrategyBase to accommodate message bus handler deployments.
    /// </summary>
    public class InitializationStrategyMessageBusHandler : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the channels to monitor on the message bus system.
        /// </summary>
        public ICollection<IChannel> ChannelsToMonitor { get; set; }

        /// <summary>
        /// Gets or sets the worker count of the handler harness.
        /// </summary>
        public int WorkerCount { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategyMessageBusHandler
                          {
                              WorkerCount = this.WorkerCount,
                              ChannelsToMonitor = this.ChannelsToMonitor.OfType<SimpleChannel>().Select(_ => (IChannel)new SimpleChannel(_.Name)).ToList()
                          };
            return ret;
        }
    }
}
