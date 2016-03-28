// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateFile.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Security;

    /// <summary>
    /// Model object to hold necessary information to inflate a certificate on a machine.
    /// </summary>
    public class CertificateFile
    {
        /// <summary>
        /// Gets or sets the name of the certificate.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the password of the certificate for installation.
        /// </summary>
        public SecureString CertificatePassword { get; set; }

        /// <summary>
        /// Gets or sets the bytes of the certificate's PFX file.
        /// </summary>
        public byte[] FileBytes { get; set; }

        /// <summary>
        /// Generates a file to write the bytes to.
        /// </summary>
        /// <returns>File name to use for the certificate.</returns>
        public string GenerateFileName()
        {
            return this.Name + ".pfx";
        }
    }
}
