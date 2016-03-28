// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyDnsEntry.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Custom extension of the InitializationStrategyBase to accommodate adding a DNS entry against the private IP address.
    /// </summary>
    public class InitializationStrategyDnsEntry : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets DNS entry to be applied to the public IP address of the created instance.
        /// </summary>
        public string PublicDnsEntry { get; set; }

        /// <summary>
        /// Gets or sets DNS entry to be applied to the private IP address of the created instance.
        /// </summary>
        public string PrivateDnsEntry { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategyDnsEntry
                          {
                              PublicDnsEntry = this.PublicDnsEntry,
                              PrivateDnsEntry = this.PrivateDnsEntry
                          };
            return ret;
        }
    }
}