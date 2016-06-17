// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategySelfHost.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
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
        /// Gets or sets the primary DNS access point of the web deployment.
        /// </summary>
        public string SelfHostDns { get; set; }

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
                              SelfHostDns = this.SelfHostDns,
                              ScheduledTaskAccount = this.ScheduledTaskAccount
                          };

            return ret;
        }
    }
}
