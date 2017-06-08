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
        /// Initializes a new instance of the <see cref="CertificateDetails"/> class.
        /// </summary>
        /// <param name="name">Name of the certificate.</param>
        /// <param name="encryptedBase64Bytes">Encrypted bytes as a Base 64 encoded string.</param>
        /// <param name="encryptedPassword">Encrypted password.</param>
        /// <param name="encryptingCertificateLocator">Locator for encrypting certificate.</param>
        public CertificateDetails(string name, string encryptedBase64Bytes, string encryptedPassword, CertificateLocator encryptingCertificateLocator)
        {
            this.Name = name;
            this.EncryptedBase64Bytes = encryptedBase64Bytes;
            this.EncryptedPassword = encryptedPassword;
            this.EncryptingCertificateLocator = encryptingCertificateLocator;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the encrypted password.
        /// </summary>
        public string EncryptedPassword { get; private set; }

        /// <summary>
        /// Gets the bytes in Base64 format and encrypted.
        /// </summary>
        public string EncryptedBase64Bytes { get; private set; }

        /// <summary>
        /// Gets the locator for the certificate used to encrypt the password and bytes of the certificate.
        /// </summary>
        public CertificateLocator EncryptingCertificateLocator { get; private set; }

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
    }
}