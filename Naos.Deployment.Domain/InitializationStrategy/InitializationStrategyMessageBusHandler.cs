// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyMessageBusHandler.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Leaving for now.")]
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
                              ChannelsToMonitor = this.ChannelsToMonitor.OfType<SimpleChannel>().Select(_ => (IChannel)new SimpleChannel(_.Name)).ToList(),
                          };
            return ret;
        }
    }
}
