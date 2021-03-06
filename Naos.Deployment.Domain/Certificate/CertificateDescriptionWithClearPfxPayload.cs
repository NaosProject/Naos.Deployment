﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateDescriptionWithClearPfxPayload.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Type;

    /// <summary>
    /// Implementation of <see cref="CertificateDescription"/> that also contains a PFX payload and a clear text password.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pfx", Justification = "Spelling/name is correct.")]
    public class CertificateDescriptionWithClearPfxPayload : CertificateDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateDescriptionWithClearPfxPayload"/> class.
        /// </summary>
        /// <param name="friendlyName">Friendly name of the certificate (does NOT have to match the "Common Name").</param>
        /// <param name="thumbprint">Thumbprint of the certificate.</param>
        /// <param name="validityWindowInUtc">Date range that the certificate is valid.</param>
        /// <param name="certificateAttributes">Attributes of the certificate.</param>
        /// <param name="pfxBytes">Bytes of the certificate's PFX file.</param>
        /// <param name="pfxPasswordInClearText">Password of the certificate's PFX file in clear text.</param>
        /// <param name="certificateSigningRequestPemEncoded">Optional PEM Encoded certificate signing request (default will be NULL).</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "pfx", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pem", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "bytes", Justification = "Spelling/name is correct.")]
        public CertificateDescriptionWithClearPfxPayload(string friendlyName, string thumbprint, UtcDateTimeRangeInclusive validityWindowInUtc, Dictionary<string, string> certificateAttributes, byte[] pfxBytes, string pfxPasswordInClearText, string certificateSigningRequestPemEncoded = null)
            : base(friendlyName, thumbprint, validityWindowInUtc, certificateAttributes, certificateSigningRequestPemEncoded)
        {
            new { pfxPasswordInClearText }.AsArg().Must().NotBeNullNorWhiteSpace();
            new { pfxBytes }.AsArg().Must().NotBeNull();

            this.PfxBytes = pfxBytes;
            this.PfxPasswordInClearText = pfxPasswordInClearText;
        }

        /// <summary>
        /// Gets the password of the certificate's PFX file in clear text.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pfx", Justification = "Spelling/name is correct.")]
        public string PfxPasswordInClearText { get; private set; }

        /// <summary>
        /// Gets the bytes of the certificate's PFX file.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pfx", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Keeping as array on purpose because it eliminates any confusion and these are small so contiguous memory isn't an issue.")]
        public byte[] PfxBytes { get; private set; }

        /// <summary>
        /// Converts to the version with an encrypted payload <see cref="CertificateDescriptionWithEncryptedPfxPayload"/>.
        /// </summary>
        /// <param name="encryptingCertificateLocator"><see cref="CertificateLocator"/> of the certificate to encrypt with.</param>
        /// <returns>Encrypted payload version of object using provided certificate information.</returns>
        public CertificateDescriptionWithEncryptedPfxPayload ToEncryptedVersion(CertificateLocator encryptingCertificateLocator)
        {
            var encryptedPassword = Encryptor.Encrypt(this.PfxPasswordInClearText, encryptingCertificateLocator);

            var certificateFileBase64 = Convert.ToBase64String(this.PfxBytes);
            var encryptedFileBase64 = Encryptor.Encrypt(certificateFileBase64, encryptingCertificateLocator);

            var ret = new CertificateDescriptionWithEncryptedPfxPayload(this.FriendlyName, this.Thumbprint, this.ValidityWindowInUtc, this.CertificateAttributes, encryptingCertificateLocator, encryptedFileBase64, encryptedPassword, this.CertificateSigningRequestPemEncoded);

            return ret;
        }
    }
}
