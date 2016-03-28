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
        /// Gets or sets the thumbprint of the certificate.
        /// </summary>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the certificate is valid.
        /// </summary>
        public bool CertificateIsValid { get; set; }
    }
}
