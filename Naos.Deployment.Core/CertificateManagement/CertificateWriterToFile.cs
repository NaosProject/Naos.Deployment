// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateWriterToFile.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Implementation using a text file of <see cref="IPersistCertificates"/>.
    /// </summary>
    public class CertificateWriterToFile : IPersistCertificates
    {
        private readonly object fileSync = new object();

        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateWriterToFile"/> class.
        /// </summary>
        /// <param name="filePath">Path to save all state to.</param>
        public CertificateWriterToFile(string filePath)
        {
            this.filePath = filePath;
        }

        /// <inheritdoc />
        public async Task PersistCertficateAsync(CertificateDescriptionWithClearPfxPayload certificateToLoad, CertificateLocator encryptingCertificateLocator)
        {
            var newCert = certificateToLoad.ToEncryptedVersion(encryptingCertificateLocator);
            await this.PersistCertficateAsync(newCert);
        }

        /// <inheritdoc />
        public async Task PersistCertficateAsync(CertificateDescriptionWithEncryptedPfxPayload certificate)
        {
            lock (this.fileSync)
            {
                var fileContentsRead = File.ReadAllText(this.filePath);
                var certificateCollection = fileContentsRead.FromJson<CertificateCollection>();

                var idxToDelete = certificateCollection.Certificates.FindIndex(_ => string.Equals(_.FriendlyName, certificate.FriendlyName, StringComparison.CurrentCultureIgnoreCase));
                if (idxToDelete != -1)
                {
                    certificateCollection.Certificates.RemoveAt(idxToDelete);
                }

                certificateCollection.Certificates.Add(certificate);

                var fileContentsWrite = certificateCollection.ToJson();
                File.WriteAllText(this.filePath, fileContentsWrite);
            }

            await Task.Run(() => { });
        }
    }
}
