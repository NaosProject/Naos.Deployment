// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyIis.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    /// <summary>
    /// Custom extension of the InitializationStrategyBase to accommodate web service/site deployments.
    /// </summary>
    public class InitializationStrategyIis : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the primary DNS access point of the web deployment.
        /// </summary>
        public string PrimaryDns { get; set; }

        /// <summary>
        /// Gets or sets the name of the SSL certificate to lookup and use.
        /// </summary>
        public string SslCertificateName { get; set; }

        /// <summary>
        /// Gets or sets the application pool start mode.
        /// </summary>
        public ApplicationPoolStartMode AppPoolStartMode { get; set; }

        /// <summary>
        /// Gets or sets the auto start provider (if any).
        /// </summary>
        public AutoStartProvider AutoStartProvider { get; set; }

        /// <inheritdoc />
        public override InitializationStrategyBase Clone()
        {
            var ret = new InitializationStrategyIis
                          {
                              AutoStartProvider =
                                  (AutoStartProvider)this.AutoStartProvider.Clone(),
                              AppPoolStartMode = this.AppPoolStartMode,
                              SslCertificateName = this.SslCertificateName,
                              PrimaryDns = this.PrimaryDns
                          };
            return ret;
        }
    }
}
