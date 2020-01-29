// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverFromFile.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Json;

    /// <summary>
    /// Implementation using a text file of IGetCertificates.
    /// </summary>
    public class CertificateRetrieverFromFile : IGetCertificates
    {
        private static readonly IStringDeserialize Serializer = new ObcJsonSerializer(typeof(NaosDeploymentCoreJsonConfiguration), UnregisteredTypeEncounteredStrategy.Attempt);

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
        public Task<CertificateDescriptionWithClearPfxPayload> GetCertificateByNameAsync(string name)
        {
            CertificateDescriptionWithClearPfxPayload certDetails;
            lock (this.fileSync)
            {
                var fileContents = File.ReadAllText(this.filePath);
                var certificateCollection = Serializer.Deserialize<CertificateCollection>(fileContents);
                var certificateDetails = certificateCollection.Certificates.SingleOrDefault(_ => string.Equals(_.FriendlyName, name, StringComparison.CurrentCultureIgnoreCase));

                certDetails = certificateDetails?.ToDecryptedVersion();
            }

            return Task.FromResult(certDetails);
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> GetAllCertificateNamesAsync()
        {
            lock (this.fileSync)
            {
                var fileContents = File.ReadAllText(this.filePath);
                var certificateCollection = Serializer.Deserialize<CertificateCollection>(fileContents);
                IReadOnlyCollection<string> certificateNames = certificateCollection.Certificates.Select(_ => _.FriendlyName).ToList();
                return Task.FromResult(certificateNames);
            }
        }
    }
}