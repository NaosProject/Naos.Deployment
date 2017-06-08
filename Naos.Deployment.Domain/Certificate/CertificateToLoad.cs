// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateToLoad.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.IO;

    using Spritely.Recipes;

    /// <summary>
    /// Temporal model object to hold a cert that is not yet encrypted.
    /// </summary>
    public class CertificateToLoad
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateToLoad"/> class.
        /// </summary>
        /// <param name="name">Name of the certificate.</param>
        /// <param name="filePath">Local path of the certificate file.</param>
        /// <param name="passwordInClearText">Certificate file password in clear text (to be encrypted).</param>
        /// <param name="encryptingCertificateLocator">Certificate locator on the running machine that will be used to encrypt the data.</param>
        public CertificateToLoad(string name, string filePath, string passwordInClearText, CertificateLocator encryptingCertificateLocator)
        {
            new { name, filePath, passwordInClearText }.Must().NotBeNull().And().NotBeWhiteSpace().OrThrowFirstFailure();
            new { encryptingCertificateLocator }.Must().NotBeNull().OrThrowFirstFailure();
            
            this.Name = name;
            this.FilePath = filePath;
            this.PasswordInClearText = passwordInClearText;
            this.EncryptingCertificateLocator = encryptingCertificateLocator;
        }

        /// <summary>
        /// Gets the name of the certificate.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the local path of the certificate file.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets the certificate file password in clear text (to be encrypted).
        /// </summary>
        public string PasswordInClearText { get; private set; }

        /// <summary>
        /// Gets the certificate locator on the running machine that will be used to encrypt the data.
        /// </summary>
        public CertificateLocator EncryptingCertificateLocator { get; private set; }

        /// <summary>
        /// Convert to a <see cref="CertificateDetails"/> object.
        /// </summary>
        /// <returns><see cref="CertificateDetails"/> version with payload(s) encrypted.</returns>
        public CertificateDetails ToCertificateDetails()
        {
            var encryptedPassword = Encryptor.Encrypt(this.PasswordInClearText, this.EncryptingCertificateLocator);

            var certificateBytes = File.ReadAllBytes(this.FilePath);
            var certificateFileBase64 = Convert.ToBase64String(certificateBytes);
            var encryptedFileBase64 = Encryptor.Encrypt(certificateFileBase64, this.EncryptingCertificateLocator);

            var ret = new CertificateDetails(this.Name, encryptedFileBase64, encryptedPassword, this.EncryptingCertificateLocator);

            return ret;
        }
    }
}