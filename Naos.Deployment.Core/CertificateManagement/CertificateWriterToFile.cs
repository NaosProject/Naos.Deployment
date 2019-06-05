// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateWriterToFile.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Serialization.Domain;
    using Naos.Serialization.Json;

    using static System.FormattableString;

    /// <summary>
    /// Implementation using a text file of <see cref="IPersistCertificates"/>.
    /// </summary>
    public class CertificateWriterToFile : IPersistCertificates
    {
        private static readonly ISerializeAndDeserialize Serializer = new NaosJsonSerializer(typeof(NaosDeploymentCoreJsonConfiguration), UnregisteredTypeEncounteredStrategy.Attempt);

        private readonly object fileSync = new object();

        private readonly string filePath;

        /// <summary>
        /// Creates a new persistence file to use with <see cref="CertificateWriterToFile" /> instances.
        /// </summary>
        /// <param name="filePath">Path to save all state to.</param>
        public static void Create(string filePath)
        {
            if (File.Exists(filePath))
            {
                throw new ArgumentException(Invariant($"{nameof(CertificateWriterToFile)} cannot create because there is already a file at: {filePath}"));
            }

            var initialObject = new CertificateCollection();
            var text = Serializer.SerializeToString(initialObject);
            File.WriteAllText(filePath, text);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateWriterToFile"/> class.
        /// </summary>
        /// <param name="filePath">Path to save all state to.</param>
        public CertificateWriterToFile(string filePath)
        {
            this.filePath = filePath;
        }

        /// <inheritdoc />
        public async Task PersistCertificateAsync(CertificateDescriptionWithClearPfxPayload certificate, CertificateLocator encryptingCertificateLocator)
        {
            var newCert = certificate.ToEncryptedVersion(encryptingCertificateLocator);
            await this.PersistCertificateAsync(newCert);
        }

        /// <inheritdoc />
        public async Task PersistCertificateAsync(CertificateDescriptionWithEncryptedPfxPayload certificate)
        {
            lock (this.fileSync)
            {
                var fileContentsRead = File.ReadAllText(this.filePath);
                var certificateCollection = Serializer.Deserialize<CertificateCollection>(fileContentsRead) ?? new CertificateCollection();
                if (certificateCollection.Certificates == null)
                {
                    certificateCollection.Certificates = new List<CertificateDescriptionWithEncryptedPfxPayload>();
                }

                var idxToDelete = certificateCollection.Certificates.FindIndex(_ => string.Equals(_.FriendlyName, certificate.FriendlyName, StringComparison.CurrentCultureIgnoreCase));
                if (idxToDelete != -1)
                {
                    certificateCollection.Certificates.RemoveAt(idxToDelete);
                }

                certificateCollection.Certificates.Add(certificate);

                var fileContentsWrite = Serializer.SerializeToString(certificateCollection);
                File.WriteAllText(this.filePath, fileContentsWrite);
            }

            await Task.Run(() => { });
        }
    }
}
