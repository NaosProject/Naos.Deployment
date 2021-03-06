﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateContainer.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
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
        /// Gets or sets the last modified date time in UTC.
        /// </summary>
        public DateTime RecordLastModifiedUtc { get; set; }
    }
}
