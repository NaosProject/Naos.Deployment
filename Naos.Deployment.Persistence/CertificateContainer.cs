// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateContainer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Persistence
{
    using System;

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
        /// Gets or sets the <see cref="CertificateDescriptionWithEncryptedPfxPayload"/>.
        /// </summary>
        public CertificateDescriptionWithEncryptedPfxPayload Certificate { get; set; }

        /// <summary>
        /// Gets or sets the last updated date time in UTC.
        /// </summary>
        public DateTime LastUpdatedUtc { get; set; }
    }
}
