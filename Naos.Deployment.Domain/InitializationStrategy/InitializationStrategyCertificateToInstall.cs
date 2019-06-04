// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyCertificateToInstall.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Security.Cryptography.X509Certificates;

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
        /// Gets or sets the location to install the certificate.
        /// </summary>
        public StoreLocation? StoreLocation { get; set; }

        /// <summary>
        /// Gets or sets the store name to install the certificate.
        /// </summary>
        public StoreName? StoreName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to install the certificate in a way that allows it be exported.
        /// </summary>
        public bool InstallExportable { get; set; }

        /// <summary>
        /// Gets or sets the account to grant private key access to on the certificate (null will skip).
        /// </summary>
        public string AccountToGrantPrivateKeyAccess { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategyCertificateToInstall { CertificateToInstall = this.CertificateToInstall, StoreLocation = this.StoreLocation, StoreName = this.StoreName, InstallExportable = this.InstallExportable, AccountToGrantPrivateKeyAccess = this.AccountToGrantPrivateKeyAccess };
            return ret;
        }
    }
}