// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateContainer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Persistence
{
    using Naos.Deployment.Domain;

    /// <summary>
    /// Container object to hold a certificate manager and save it in Mongo.
    /// </summary>
    public class CertificateContainer
    {
        /// <summary>
        /// Gets or sets the ID of the record.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the certificates for an environment.
        /// </summary>
        public CertificateDetails[] Certificates { get; set; }
    }
}
