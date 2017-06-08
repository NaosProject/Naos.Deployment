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
    /// Implementation using a text file of <see cref="ILoadCertificates"/>.
    /// </summary>
    public class CertificateWriterToFile : ILoadCertificates
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
        public async Task LoadCertficateAsync(CertificateToLoad certificateToLoad)
        {
            var newCert = certificateToLoad.ToCertificateDetails();
            await this.LoadCertficateAsync(newCert);
        }

        /// <inheritdoc />
        public async Task LoadCertficateAsync(CertificateDetails certificate)
        {
            lock (this.fileSync)
            {
                var fileContentsRead = File.ReadAllText(this.filePath);
                var certificateCollection = fileContentsRead.FromJson<CertificateCollection>();

                var idxToDelete = certificateCollection.Certificates.FindIndex(_ => string.Equals(_.Name, certificate.Name, StringComparison.CurrentCultureIgnoreCase));
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
