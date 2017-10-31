// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverFromMongo.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.Serialization.Bson;

    using Spritely.ReadModel;
    using Spritely.Recipes;

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
            new { certificateContainerQueries }.Must().NotBeNull().OrThrowFirstFailure();

            this.certificateContainerQueries = certificateContainerQueries;

            BsonConfigurationManager.Configure<DeploymentBsonConfiguration>();
        }

        /// <inheritdoc />
        public async Task<CertificateDescriptionWithClearPfxPayload> GetCertificateByNameAsync(string name)
        {
            var certificateContainer = await this.certificateContainerQueries.GetOneAsync(_ => string.Equals(_.Id, name, StringComparison.InvariantCultureIgnoreCase));

            var certificateDetails = certificateContainer?.Certificate;

            var certDetails = certificateDetails?.ToDecryptedVersion();

            return certDetails;
        }
    }
}