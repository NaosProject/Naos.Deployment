// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateDetails.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Security;

    /// <summary>
    /// Class to allow TheSafe to be serialized and deserialized but still provide a CertificateDetails object.
    /// </summary>
    public class CertificateDetails
    {
        /// <summary>
        /// Converts the container to a certificate details class.
        /// </summary>
        /// <param name="stringDecryptor">Function to decrypt the encrypted password and convert it into a <see cref="SecureString"/>.</param>
        /// <returns>Converted details version.</returns>
        public CertificateFile ToCertificateDetails(Func<string, SecureString> stringDecryptor)
        {
            var ret = new CertificateFile
                          {
                              Name = this.Name,
                              CertificatePassword = stringDecryptor(this.EncryptedPassword),
                              FileBytes = Convert.FromBase64String(this.Base64Bytes),
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
        /// Gets or sets the bytes in Base64 format.
        /// </summary>
        public string Base64Bytes { get; set; }
    }
}