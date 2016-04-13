// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverFromFile.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Implementation using a text file of IGetCertificates.
    /// </summary>
    public class CertificateRetrieverFromFile : IGetCertificates
    {
        private readonly object fileSync = new object();

        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateRetrieverFromFile"/> class.
        /// </summary>
        /// <param name="filePath">Path to save all state to.</param>
        public CertificateRetrieverFromFile(string filePath)
        {
            this.filePath = filePath;
        }

        /// <inheritdoc />
        public Task<CertificateFile> GetCertificateByNameAsync(string name)
        {
            CertificateFile certDetails;
            lock (this.fileSync)
            {
                var fileContents = File.ReadAllText(this.filePath);
                var certificateCollection = Serializer.Deserialize<CertificateCollection>(fileContents);
                var certificateDetails = certificateCollection.Certificates.SingleOrDefault(_ => string.Equals(_.Name, name, StringComparison.CurrentCultureIgnoreCase));

                certDetails = certificateDetails == null ? null : certificateDetails.ToCertificateFile();
            }

            return Task.FromResult(certDetails);
        }
    }
}
