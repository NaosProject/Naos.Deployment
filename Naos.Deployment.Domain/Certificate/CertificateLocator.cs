// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateLocator.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Model object to hold necessary information to find a certificate on a machine.
    /// </summary>
    public class CertificateLocator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateLocator"/> class.
        /// </summary>
        /// <param name="certificateThumbprint">Thumbprint of certificate.</param>
        /// <param name="certificateIsValid">Value indicating whether or not the certificate is "valid".</param>
        public CertificateLocator(string certificateThumbprint, bool certificateIsValid)
        {
            this.CertificateIsValid = certificateIsValid;
            this.CertificateThumbprint = certificateThumbprint;
        }

        /// <summary>
        /// Gets the thumbprint of the certificate.
        /// </summary>
        public string CertificateThumbprint { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the certificate is valid.
        /// </summary>
        public bool CertificateIsValid { get; private set; }
    }
}
