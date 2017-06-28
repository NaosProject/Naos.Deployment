// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateWriterToMongo.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;

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
            if (certificateContainerCommands == null)
            {
                throw new ArgumentNullException(nameof(certificateContainerCommands));
            }

            this.certificateContainerCommands = certificateContainerCommands;

            BsonClassMapManager.RegisterClassMaps();
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
            var container = new CertificateContainer { Id = certificate.FriendlyName, Certificate = certificate, LastUpdatedUtc = DateTime.UtcNow };

            await this.certificateContainerCommands.AddOrUpdateOneAsync(container);
        }
    }
}