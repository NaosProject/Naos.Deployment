// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateDescriptionWithEncryptedPfxPayload.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;

    using OBeautifulCode.DateTime;

    using Spritely.Recipes;

    /// <summary>
    /// Implementation of <see cref="CertificateDescription"/> that also contains an encrypted PFX payload and an encrypted password with the certificate details used to encrypt.
    /// </summary>
    public class CertificateDescriptionWithEncryptedPfxPayload : CertificateDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateDescriptionWithEncryptedPfxPayload"/> class.
        /// </summary>
        /// <param name="friendlyName">Friendly name of the certificate (does NOT have to match the "Common Name").</param>
        /// <param name="thumbprint">Thumbprint of the certificate.</param>
        /// <param name="validityWindowInUtc">Date range that the certificate is valid.</param>
        /// <param name="certificateAttributes">Attributes of the certificate.</param>
        /// <param name="encryptingCertificateLocator">Locator for the certificate used to encrypt the password and bytes of the certificate.</param>
        /// <param name="encryptedFileBase64">Bytes of the PFX file in Base64 format and encrypted.</param>
        /// <param name="encryptedPfxPassword">Encrypted password of the PFX file.</param>
        /// <param name="certificateSigningRequestPemEncoded">Optional PEM Encoded certificate signing request (default will be NULL).</param>
        public CertificateDescriptionWithEncryptedPfxPayload(string friendlyName, string thumbprint, DateTimeRangeInclusive validityWindowInUtc, Dictionary<string, string> certificateAttributes, CertificateLocator encryptingCertificateLocator, string encryptedFileBase64, string encryptedPfxPassword, string certificateSigningRequestPemEncoded = null)
            : base(friendlyName, thumbprint, validityWindowInUtc, certificateAttributes, certificateSigningRequestPemEncoded)
        {
            new { encryptedFileBase64, encryptedPassword = encryptedPfxPassword }.Must().NotBeNull().And().NotBeWhiteSpace().OrThrowFirstFailure();
            new { encryptingCertificateLocator }.Must().NotBeNull().OrThrowFirstFailure();

            this.EncryptingCertificateLocator = encryptingCertificateLocator;
            this.EncryptedBase64EncodedPfxBytes = encryptedFileBase64;
            this.EncryptedPfxPassword = encryptedPfxPassword;
        }

        /// <summary>
        /// Gets the locator for the certificate used to encrypt the password and bytes of the certificate.
        /// </summary>
        public CertificateLocator EncryptingCertificateLocator { get; private set; }

        /// <summary>
        /// Gets the bytes of the PFX file in Base64 format and encrypted.
        /// </summary>
        public string EncryptedBase64EncodedPfxBytes { get; private set; }

        /// <summary>
        /// Gets the encrypted password of the PFX file.
        /// </summary>
        public string EncryptedPfxPassword { get; private set; }

        /// <summary>
        /// Converts the certificate details into a usable certificate file (decrypting the file and password).
        /// </summary>
        /// <returns>Converted details version.</returns>
        public CertificateDescriptionWithClearPfxPayload ToDecryptedVersion()
        {
            var decryptedPassword = Encryptor.Decrypt(this.EncryptedPfxPassword, this.EncryptingCertificateLocator);
            var decryptedBase64 = Encryptor.Decrypt(this.EncryptedBase64EncodedPfxBytes, this.EncryptingCertificateLocator);
            var certificateBytes = Convert.FromBase64String(decryptedBase64);

            var ret = new CertificateDescriptionWithClearPfxPayload(this.FriendlyName, this.Thumbprint, this.ValidityWindowInUtc, this.CertificateAttributes, certificateBytes, decryptedPassword, this.CertificateSigningRequestPemEncoded);

            return ret;
        }
    }
}