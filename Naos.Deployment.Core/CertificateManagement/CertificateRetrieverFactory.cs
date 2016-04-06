// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverFactory.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;

    using Naos.Deployment.Core.CertificateManagement;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;

    /// <summary>
    /// Factory for creating certificate retrievers.
    /// </summary>
    public static class CertificateRetrieverFactory
    {
        /// <summary>
        /// Creates a certificate retriever from configuration provided.
        /// </summary>
        /// <param name="environment">Environment to create a retriever for.</param>
        /// <param name="certificateRetrieverConfigurationBase">Configuration to use when creating a certificate retriever.</param>
        /// <returns>An implementation of the <see cref="IGetCertificates"/>.</returns>
        public static IGetCertificates Create(string environment, CertificateRetrieverConfigurationBase certificateRetrieverConfigurationBase)
        {
            IGetCertificates ret;

            if (certificateRetrieverConfigurationBase is CertificateRetrieverConfigurationFile)
            {
                var configAsFile = (CertificateRetrieverConfigurationFile)certificateRetrieverConfigurationBase;
                ret = new CertificateRetrieverFromFile(configAsFile.FilePath);
            }
            else if (certificateRetrieverConfigurationBase is CertificateRetrieverConfigurationDatabase)
            {
                var configAsDb = (CertificateRetrieverConfigurationDatabase)certificateRetrieverConfigurationBase;
                var certificateContainerQueries = configAsDb.Database.GetQueriesInterface<CertificateContainer>();
                ret = new CertificateRetrieverFromMongo(environment, certificateContainerQueries);
            }
            else
            {
                throw new NotSupportedException("Configuration is not valid: " + Serializer.Serialize(certificateRetrieverConfigurationBase));
            }

            return ret;
        }
    }
}