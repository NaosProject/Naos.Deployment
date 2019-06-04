// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyIis.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;
    using System.Linq;

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
        /// Gets or sets the host headers to use for HTTPS; DEFAULT will be none.
        /// </summary>
        public IReadOnlyCollection<HttpsBinding> HttpsBindings { get; set; }

        /// <summary>
        /// Gets or sets the host header to use for HTTP; DEFAULT will remove the HTTP binding.
        /// </summary>
        public string HostHeaderForHttpBinding { get; set; }

        /// <summary>
        /// Gets or sets the account to run the application pool as.
        /// </summary>
        public string AppPoolAccount { get; set; }

        /// <summary>
        /// Gets or sets the application pool start mode.
        /// </summary>
        public ApplicationPoolStartMode AppPoolStartMode { get; set; }

        /// <summary>
        /// Gets or sets the auto start provider (if any).
        /// </summary>
        public AutoStartProvider AutoStartProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to enable SNI.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sni", Justification = "Spelling/name is correct.")]
        public bool EnableSni { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategyIis
                          {
                              AutoStartProvider = (AutoStartProvider)this.AutoStartProvider.Clone(),
                              AppPoolAccount = this.AppPoolAccount,
                              AppPoolStartMode = this.AppPoolStartMode,
                              PrimaryDns = this.PrimaryDns,
                              HttpsBindings = this.HttpsBindings?.Select(_ => _.Clone()).ToList(),
                              HostHeaderForHttpBinding = this.HostHeaderForHttpBinding,
                              EnableSni = this.EnableSni,
                          };

            return ret;
        }
    }

    /// <summary>
    /// HTTPS binding.
    /// </summary>
    public class HttpsBinding
    {
        /// <summary>
        /// Gets or sets the host header to use with the .
        /// </summary>
        public string HostHeader { get; set; }

        /// <summary>
        /// Gets or sets the name of the SSL certificate to lookup and use.
        /// </summary>
        public string SslCertificateName { get; set; }

        /// <summary>
        /// Clone method.
        /// </summary>
        /// <returns>Cloned binding.</returns>
        public HttpsBinding Clone()
        {
            var ret = new HttpsBinding { HostHeader = this.HostHeader, SslCertificateName = this.SslCertificateName, };
            return ret;
        }
    }
}
