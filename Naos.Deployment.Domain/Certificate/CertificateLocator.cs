// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateLocator.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Model object to hold necessary information to find a certificate on a machine.
    /// </summary>
    public class CertificateLocator
    {
        public const StoreName DefaultCertificateStoreName = StoreName.My;

        public const StoreLocation DefaultCertificateStoreLocation = StoreLocation.LocalMachine;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateLocator"/> class.
        /// </summary>
        /// <param name="certificateThumbprint">Thumbprint of a certificate.</param>
        /// <param name="certificateIsValid">Value indicating whether or not the certificate is "valid".</param>
        /// <param name="certificateStoreName">Optional store name of certificate; DEFAULT is <see cref="StoreName.My"/>.</param>
        /// <param name="certificateStoreLocation">Optional store location of certificate; DEFAULT is <see cref="StoreLocation.LocalMachine"/>.</param>
        public CertificateLocator(string certificateThumbprint, bool certificateIsValid, StoreName certificateStoreName = DefaultCertificateStoreName, StoreLocation certificateStoreLocation = DefaultCertificateStoreLocation)
        {
            if (certificateThumbprint == null)
            {
                throw new ArgumentException("Must supply a certificate thumbprint to use for retrieving the encrypting certificate.");
            }

            this.CertificateThumbprint = certificateThumbprint;
            this.CertificateIsValid = certificateIsValid;
            this.CertificateStoreName = certificateStoreName;
            this.CertificateStoreLocation = certificateStoreLocation;
        }

        /// <summary>
        /// Gets the thumbprint of the certificate.
        /// </summary>
        public string CertificateThumbprint { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the certificate is valid.
        /// </summary>
        public bool CertificateIsValid { get; private set; }

        /// <summary>
        /// Gets the store name of the certificate.
        /// </summary>
        public StoreName CertificateStoreName { get; private set; }

        /// <summary>
        /// Gets the store location of the certificate.
        /// </summary>
        public StoreLocation CertificateStoreLocation { get; private set; }
    }
}
