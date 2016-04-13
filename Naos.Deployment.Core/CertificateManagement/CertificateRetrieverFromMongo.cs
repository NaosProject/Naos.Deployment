// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverFromMongo.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;

    using Spritely.ReadModel;

    /// <summary>
    /// Implementation using Mongo of <see cref="IGetCertificates"/>.
    /// </summary>
    public class CertificateRetrieverFromMongo : IGetCertificates
    {
        private readonly string environment;

        private readonly IQueries<CertificateContainer> certificateContainerQueries;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateRetrieverFromMongo"/> class.
        /// </summary>
        /// <param name="environment">Environment executing against.</param>
        /// <param name="certificateContainerQueries">Query interface for retrieving the certificates.</param>
        public CertificateRetrieverFromMongo(string environment, IQueries<CertificateContainer> certificateContainerQueries)
        {
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }

            if (certificateContainerQueries == null)
            {
                throw new ArgumentNullException("certificateContainerQueries");
            }

            this.environment = environment;
            this.certificateContainerQueries = certificateContainerQueries;

            BsonClassMapManager.RegisterClassMaps();
        }

        /// <inheritdoc />
        public async Task<CertificateFile> GetCertificateByNameAsync(string name)
        {
            var certificateContainer =
                await this.certificateContainerQueries.GetOneAsync(_ => _.Environment == this.environment);

            var certificateDetails =
                certificateContainer.Certificates.SingleOrDefault(
                    _ => string.Equals(_.Name, name, StringComparison.CurrentCultureIgnoreCase));

            var certDetails = certificateDetails == null ? null : certificateDetails.ToCertificateFile();
            return certDetails;
        }
    }
}