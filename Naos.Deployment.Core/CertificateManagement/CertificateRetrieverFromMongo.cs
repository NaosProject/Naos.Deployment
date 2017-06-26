// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverFromMongo.cs" company="Naos">
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
    /// Implementation using Mongo of <see cref="IGetCertificates"/>.
    /// </summary>
    public class CertificateRetrieverFromMongo : IGetCertificates
    {
        private readonly IQueries<CertificateContainer> certificateContainerQueries;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateRetrieverFromMongo"/> class.
        /// </summary>
        /// <param name="certificateContainerQueries">Query interface for retrieving the certificates.</param>
        public CertificateRetrieverFromMongo(IQueries<CertificateContainer> certificateContainerQueries)
        {
            if (certificateContainerQueries == null)
            {
                throw new ArgumentNullException(nameof(certificateContainerQueries));
            }

            this.certificateContainerQueries = certificateContainerQueries;

            BsonClassMapManager.RegisterClassMaps();
        }

        /// <inheritdoc />
        public async Task<CertificateDescriptionWithClearPfxPayload> GetCertificateByNameAsync(string name)
        {
            CertificateContainer certificateContainer = await this.certificateContainerQueries.GetOneAsync(_ => _.Id.ToLowerInvariant() == name.ToLowerInvariant());

            var certificateDetails = certificateContainer?.Certificate;

            var certDetails = certificateDetails?.ToDecryptedVersion();

            return certDetails;
        }
    }
}