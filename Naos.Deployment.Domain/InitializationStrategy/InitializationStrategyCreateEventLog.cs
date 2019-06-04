// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyCreateEventLog.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Diagnostics;

    /// <summary>
    /// Custom extension of the InitializationStrategyBase to accommodate adding a DNS entry against the private IP address.
    /// </summary>
    public class InitializationStrategyCreateEventLog : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the <see cref="EventLog.Source" /> to setup on the provided <see cref="LogName" />.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventLog.Log" /> to setup.
        /// </summary>
        public string LogName { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategyCreateEventLog
                          {
                              Source = this.Source,
                              LogName = this.LogName,
                          };
            return ret;
        }
    }
}