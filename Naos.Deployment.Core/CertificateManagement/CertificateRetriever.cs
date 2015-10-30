// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetriever.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.IO;
    using System.Linq;

    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;

    /// <summary>
    /// Implementation of IManageCertificates
    /// </summary>
    public class CertificateRetriever : IGetCertificates
    {
        private readonly object fileSync = new object();
        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateRetriever"/> class.
        /// </summary>
        /// <param name="filePath">Path to save all state to.</param>
        public CertificateRetriever(string filePath)
        {
            this.filePath = filePath;
        }

        /// <inheritdoc />
        public CertificateDetails GetCertificateByName(string name)
        {
            lock (this.fileSync)
            {
                var fileContents = File.ReadAllText(this.filePath);
                var certificateManager = Serializer.Deserialize<CertificateManager>(fileContents);
                var certContainer = certificateManager.Certificates.SingleOrDefault(_ => string.Equals(_.Name, name, StringComparison.CurrentCultureIgnoreCase));
                var certDetails = certContainer == null ? null : certContainer.ToCertificateDetails();
                return certDetails;
            }
        }
    }
}
