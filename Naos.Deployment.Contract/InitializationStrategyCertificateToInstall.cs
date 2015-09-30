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

        /// <inheritdoc />
        public override InitializationStrategyBase Clone()
        {
            var ret = new InitializationStrategyCertificateToInstall { CertificateToInstall = this.CertificateToInstall };
            return ret;
        }
    }
}