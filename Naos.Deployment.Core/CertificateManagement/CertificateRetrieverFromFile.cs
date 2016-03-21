// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverFromFile.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;

    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;
    using Naos.WinRM;

    /// <summary>
    /// Implementation using a text file of IGetCertificates.
    /// </summary>
    public class CertificateRetrieverFromFile : IGetCertificates
    {
        private readonly object fileSync = new object();

        private readonly string filePath;

        private readonly CertificateLocator encryptingCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateRetrieverFromFile"/> class.
        /// </summary>
        /// <param name="filePath">Path to save all state to.</param>
        /// <param name="encryptingCertificate">Locator for the certificate installed on computer to use to decrypt password.</param>
        public CertificateRetrieverFromFile(string filePath, CertificateLocator encryptingCertificate)
        {
            this.filePath = filePath;
            this.encryptingCertificate = encryptingCertificate;
        }

        /// <inheritdoc />
        public CertificateFile GetCertificateByName(string name)
        {
            lock (this.fileSync)
            {
                var fileContents = File.ReadAllText(this.filePath);
                var certificateManager = Serializer.Deserialize<CertificateCollection>(fileContents);
                var certContainer = certificateManager.Certificates.SingleOrDefault(_ => string.Equals(_.Name, name, StringComparison.CurrentCultureIgnoreCase));
                Func<string, SecureString> stringDecryptor = encryptedInput =>
                    {
                        var decryptedText = Encryptor.Decrypt(encryptedInput, this.encryptingCertificate);
                        return MachineManager.ConvertStringToSecureString(decryptedText);
                    };

                var certDetails = certContainer == null ? null : certContainer.ToCertificateDetails(stringDecryptor);
                return certDetails;
            }
        }

        /// <summary>
        /// Class to be used to hold certificates for an environment.
        /// </summary>
        private class CertificateCollection
        {
            /// <summary>
            /// Gets or sets the certificates.
            /// </summary>
            public List<CertificateDetails> Certificates { get; set; }
        }
    }
}
