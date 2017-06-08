// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateManagementFactory.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;

    using Naos.Deployment.Core.CertificateManagement;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Factory for creating certificate retrievers.
    /// </summary>
    public static class CertificateManagementFactory
    {
        /// <summary>
        /// Creates an implementation of the <see cref="IGetCertificates"/> from configuration provided.
        /// </summary>
        /// <param name="certificateManagementConfigurationBase">Configuration to use when creating a certificate retriever.</param>
        /// <returns>An implementation of the <see cref="IGetCertificates"/>.</returns>
        public static IGetCertificates CreateReader(CertificateManagementConfigurationBase certificateManagementConfigurationBase)
        {
            IGetCertificates ret;

            if (certificateManagementConfigurationBase is CertificateManagementConfigurationFile)
            {
                var configAsFile = (CertificateManagementConfigurationFile)certificateManagementConfigurationBase;
                ret = new CertificateRetrieverFromFile(configAsFile.FilePath);
            }
            else if (certificateManagementConfigurationBase is CertificateManagementConfigurationDatabase)
            {
                var configAsDb = (CertificateManagementConfigurationDatabase)certificateManagementConfigurationBase;
                var certificateContainerQueries = configAsDb.Database.GetQueriesInterface<CertificateContainer>();
                ret = new CertificateRetrieverFromMongo(certificateContainerQueries);
            }
            else
            {
                throw new NotSupportedException($"Configuration is not valid: {certificateManagementConfigurationBase.ToJson()}");
            }

            return ret;
        }

        /// <summary>
        /// Creates an implementation of the <see cref="ILoadCertificates"/> from configuration provided.
        /// </summary>
        /// <param name="certificateManagementConfigurationBase">Configuration to use when creating a certificate writer.</param>
        /// <returns>An implementation of the <see cref="ILoadCertificates"/>.</returns>
        public static ILoadCertificates CreateWriter(CertificateManagementConfigurationBase certificateManagementConfigurationBase)
        {
            ILoadCertificates ret;

            if (certificateManagementConfigurationBase is CertificateManagementConfigurationFile)
            {
                var configAsFile = (CertificateManagementConfigurationFile)certificateManagementConfigurationBase;
                ret = new CertificateWriterToFile(configAsFile.FilePath);
            }
            else if (certificateManagementConfigurationBase is CertificateManagementConfigurationDatabase)
            {
                var configAsDb = (CertificateManagementConfigurationDatabase)certificateManagementConfigurationBase;
                var certificateContainerQueries = configAsDb.Database.GetCommandsInterface<string, CertificateContainer>();
                ret = new CertificateWriterToMongo(certificateContainerQueries);
            }
            else
            {
                throw new NotSupportedException($"Configuration is not valid: {certificateManagementConfigurationBase.ToJson()}");
            }

            return ret;
        }
    }
}