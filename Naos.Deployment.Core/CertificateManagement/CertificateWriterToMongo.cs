// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateWriterToMongo.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.Threading.Tasks;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Serialization;
    using Spritely.ReadModel;

    /// <summary>
    /// Implementation using Mongo of <see cref="IPersistCertificates"/>.
    /// </summary>
    public class CertificateWriterToMongo : IPersistCertificates
    {
        private readonly ICommands<string, CertificateContainer> certificateContainerCommands;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateWriterToMongo"/> class.
        /// </summary>
        /// <param name="certificateContainerCommands">Query interface for retrieving the certificates.</param>
        public CertificateWriterToMongo(ICommands<string, CertificateContainer> certificateContainerCommands)
        {
            new { certificateContainerCommands }.AsArg().Must().NotBeNull();

            this.certificateContainerCommands = certificateContainerCommands;

            SerializationConfigurationManager.Configure<DeploymentBsonConfiguration>();
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
            var container = new CertificateContainer { Id = certificate.FriendlyName, Certificate = certificate, RecordLastModifiedUtc = DateTime.UtcNow };

            await this.certificateContainerCommands.AddOrUpdateOneAsync(container);
        }
    }
}
