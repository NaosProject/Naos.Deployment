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
    /// Implementation using Mongo of <see cref="ILoadCertificates"/>.
    /// </summary>
    public class CertificateWriterToMongo : ILoadCertificates
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
        public async Task LoadCertficateAsync(CertificateToLoad certificateToLoad)
        {
            var newCert = certificateToLoad.ToCertificateDetails();
            await this.LoadCertficateAsync(newCert);
        }

        /// <inheritdoc />
        public async Task LoadCertficateAsync(CertificateDetails certificate)
        {
            var container = new CertificateContainer { Id = certificate.Name, Certificate = certificate };

            await this.certificateContainerCommands.AddOrUpdateOneAsync(container);
        }
    }
}