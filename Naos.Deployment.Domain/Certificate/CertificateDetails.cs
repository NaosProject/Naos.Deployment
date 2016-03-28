// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateDetails.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    using Spritely.Recipes;

    /// <summary>
    /// Class to hold necessary information to produce a certificate file.
    /// </summary>
    public class CertificateDetails
    {
        /// <summary>
        /// Converts the certificate details into a usable certificate file (decrypting the file and password).
        /// </summary>
        /// <returns>Converted details version.</returns>
        public CertificateFile ToCertificateFile()
        {
            var decryptedPassword = Encryptor.Decrypt(this.EncryptedPassword, this.EncryptingCertificateLocator).ToSecureString();
            var decryptedBase64 = Encryptor.Decrypt(this.EncryptedBase64Bytes, this.EncryptingCertificateLocator);
            var certificateBytes = Convert.FromBase64String(decryptedBase64);

            var ret = new CertificateFile
                          {
                              Name = this.Name,
                              CertificatePassword = decryptedPassword,
                              FileBytes = certificateBytes,
                          };

            return ret;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the encrypted password.
        /// </summary>
        public string EncryptedPassword { get; set; }

        /// <summary>
        /// Gets or sets the bytes in Base64 format and encrypted.
        /// </summary>
        public string EncryptedBase64Bytes { get; set; }

        /// <summary>
        /// Gets or sets the locator for the certificate used to encrypt the password and bytes of the certificate.
        /// </summary>
        public CertificateLocator EncryptingCertificateLocator { get; set; }
    }
}