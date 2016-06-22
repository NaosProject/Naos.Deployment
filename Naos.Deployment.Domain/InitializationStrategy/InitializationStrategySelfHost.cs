// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategySelfHost.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Custom extension of the DeploymentConfiguration to accommodate self host deployments.
    /// </summary>
    public class InitializationStrategySelfHost : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the name of the executable that is hosting.
        /// </summary>
        public string SelfHostExeName { get; set; }

        /// <summary>
        /// Gets or sets the DNS entries to support access of the self hosted deployment.
        /// </summary>
        public IReadOnlyCollection<DnsEntry> SelfHostSupportedDnsEntries { get; set; }

        /// <summary>
        /// Gets or sets the name of the SSL certificate to lookup and use.
        /// </summary>
        public string SslCertificateName { get; set; }

        /// <summary>
        /// Gets or sets the account to configure the scheduled task that runs the executable.
        /// </summary>
        public string ScheduledTaskAccount { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategySelfHost
                          {
                              SelfHostExeName = this.SelfHostExeName,
                              SslCertificateName = this.SslCertificateName,
                              SelfHostSupportedDnsEntries =
                                  this.SelfHostSupportedDnsEntries.Select(_ => _.Clone() as DnsEntry).ToList(),
                              ScheduledTaskAccount = this.ScheduledTaskAccount
                          };

            return ret;
        }
    }

    /// <summary>
    /// Container class for 
    /// </summary>
    public class DnsEntry : ICloneable
    {
        /// <inheritdoc />
        public object Clone()
        {
            var ret = new DnsEntry { Address = this.Address.Clone().ToString(), ShouldUpdate = this.ShouldUpdate };

            return ret;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to update the DNS entry in the DNS management system.
        /// </summary>
        public bool ShouldUpdate { get; set; }

        /// <summary>
        /// Gets or sets the DNS address to use.
        /// </summary>
        public string Address { get; set; }
    }
}
