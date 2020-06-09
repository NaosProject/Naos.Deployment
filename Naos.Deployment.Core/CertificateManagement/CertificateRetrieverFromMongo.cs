// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverFromMongo.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Bson;
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
            new { certificateContainerQueries }.AsArg().Must().NotBeNull();

            this.certificateContainerQueries = certificateContainerQueries;

            SerializationConfigurationManager.GetOrAddSerializationConfiguration(typeof(DeploymentBsonSerializationConfiguration).ToBsonSerializationConfigurationType());
        }

        /// <inheritdoc />
        public async Task<CertificateDescriptionWithClearPfxPayload> GetCertificateByNameAsync(string name)
        {
            new { name }.AsArg().Must().NotBeNullNorWhiteSpace();

            // ReSharper disable once SpecifyStringComparison - can't because the expression tree it converts to isn't supported by Mongo...
            var certificateContainer = await this.certificateContainerQueries.GetOneAsync(_ => _.Id.ToUpperInvariant() == name.ToUpperInvariant());

            var certificateDetails = certificateContainer?.Certificate;

            var certDetails = certificateDetails?.ToDecryptedVersion();

            return certDetails;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<string>> GetAllCertificateNamesAsync()
        {
            var certificateContainers = await this.certificateContainerQueries.GetAllAsync();
            var certificateNames = certificateContainers.Select(_ => _.Id).ToList();
            return certificateNames;
        }

        /// <summary>
        /// Builds a new <see cref="CertificateRetrieverFromMongo" /> from the provided connection information.
        /// </summary>
        /// <param name="database">Database connection information.</param>
        /// <returns>Built reader.</returns>
        public static CertificateRetrieverFromMongo Build(DeploymentDatabase database)
        {
            new { database }.AsArg().Must().NotBeNull();

            var ret = new CertificateRetrieverFromMongo(database.GetQueriesInterface<CertificateContainer>());
            return ret;
        }
    }
}
