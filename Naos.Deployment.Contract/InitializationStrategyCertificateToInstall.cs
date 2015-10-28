// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyCertificateToInstall.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Custom extension of the InitializationStrategyBase to accommodate adding a specific certificate to the instance.
    /// </summary>
    public class InitializationStrategyCertificateToInstall : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets certificates to install on instance.
        /// </summary>
        public string CertificateToInstall { get; set; }

        /// <summary>
        /// Gets or sets the user to grant private key access to on the certificate (null will skip).
        /// </summary>
        public string UserToGrantPrivateKeyAccess { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategyCertificateToInstall { CertificateToInstall = this.CertificateToInstall, UserToGrantPrivateKeyAccess = this.UserToGrantPrivateKeyAccess };
            return ret;
        }
    }
}