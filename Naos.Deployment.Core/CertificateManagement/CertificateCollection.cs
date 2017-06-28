// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateCollection.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System.Collections.Generic;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Class to be used to hold certificates for an environment.
    /// </summary>
    public class CertificateCollection
    {
        /// <summary>
        /// Gets or sets the certificates.
        /// </summary>
        public List<CertificateDescriptionWithEncryptedPfxPayload> Certificates { get; set; }
    }
}