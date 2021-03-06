// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateManagementFactory.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Linq;

    using MongoDB.Bson;

    using Naos.Deployment.Core.CertificateManagement;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.Deployment.Tracking;

    using OBeautifulCode.Security.Recipes;

    using static System.FormattableString;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "I prefer this layout.")]
        public static IGetCertificates CreateReader(CertificateManagementConfigurationBase certificateManagementConfigurationBase)
        {
            IGetCertificates ret;

            if (certificateManagementConfigurationBase is CertificateManagementConfigurationFile configAsFile)
            {
                ret = new CertificateRetrieverFromFile(configAsFile.FilePath);
            }
            else if (certificateManagementConfigurationBase is CertificateManagementConfigurationDatabase configAsDb)
            {
                var certificateContainerQueries = configAsDb.Database.GetQueriesInterface<CertificateContainer>();
                ret = new CertificateRetrieverFromMongo(certificateContainerQueries);
            }
            else
            {
                throw new NotSupportedException(Invariant($"Configuration is not valid: {certificateManagementConfigurationBase.ToJson()}"));
            }

            return ret;
        }

        /// <summary>
        /// Creates an implementation of the <see cref="IPersistCertificates"/> from configuration provided.
        /// </summary>
        /// <param name="certificateManagementConfigurationBase">Configuration to use when creating a certificate writer.</param>
        /// <returns>An implementation of the <see cref="IPersistCertificates"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "I prefer this layout.")]
        public static IPersistCertificates CreateWriter(CertificateManagementConfigurationBase certificateManagementConfigurationBase)
        {
            IPersistCertificates ret;

            if (certificateManagementConfigurationBase is CertificateManagementConfigurationFile configAsFile)
            {
                ret = new CertificateWriterToFile(configAsFile.FilePath);
            }
            else if (certificateManagementConfigurationBase is CertificateManagementConfigurationDatabase configAsDb)
            {
                var certificateContainerQueries = configAsDb.Database.GetCommandsInterface<string, CertificateContainer>();
                ret = new CertificateWriterToMongo(certificateContainerQueries);
            }
            else
            {
                throw new NotSupportedException(Invariant($"Configuration is not valid: {LoggingHelper.SerializeToString(certificateManagementConfigurationBase)}"));
            }

            return ret;
        }

        /// <summary>
        /// Builds a <see cref="CertificateDescriptionWithClearPfxPayload"/> by dynamically extracting additional data from the a PFX file's bytes, a friendly name and it's clear text password (can take optional PEM encoded certificate signing request).
        /// </summary>
        /// <param name="friendlyName">Friendly name of the certificate (does NOT have to match the "Common Name").</param>
        /// <param name="pfxBytes">Bytes of the certificate's PFX file.</param>
        /// <param name="pfxPasswordInClearText">Password of the certificate's PFX file in clear text.</param>
        /// <param name="certificateSigningRequestPemEncoded">Optional PEM Encoded certificate signing request (default will be NULL).</param>
        /// <returns>New <see cref="CertificateDescriptionWithClearPfxPayload"/> with additional info extracted dynamically from file.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "pfx", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pfx", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pem", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "bytes", Justification = "Spelling/name is correct.")]
        public static CertificateDescriptionWithClearPfxPayload BuildCertificateDescriptionWithClearPfxPayload(string friendlyName, byte[] pfxBytes, string pfxPasswordInClearText, string certificateSigningRequestPemEncoded = null)
        {
            var endUserCert = CertHelper.ExtractCryptographicObjectsFromPfxFile(pfxBytes, pfxPasswordInClearText).CertificateChain.GetEndUserCertFromCertChain();
            var certFields = endUserCert.GetX509Fields();
            var certFieldsAsStrings = certFields.ToDictionary(k => k.Key.ToString(), v => v.Value?.ToString());
            var certValidityPeriod = endUserCert.GetValidityPeriod();
            var thumbprint = endUserCert.GetThumbprint();

            return new CertificateDescriptionWithClearPfxPayload(
                       friendlyName,
                       thumbprint,
                       certValidityPeriod,
                       certFieldsAsStrings,
                       pfxBytes,
                       pfxPasswordInClearText,
                       certificateSigningRequestPemEncoded);
        }
    }
}
