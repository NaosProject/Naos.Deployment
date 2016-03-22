// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverFromMongo.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.Linq;
    using System.Security;

    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.WinRM;

    using Spritely.ReadModel;

    /// <summary>
    /// Implementation using Mongo of <see cref="IGetCertificates"/>.
    /// </summary>
    public class CertificateRetrieverFromMongo : IGetCertificates
    {
        private readonly object fileSync = new object();

        private readonly string environment;

        private readonly CertificateLocator encryptingCertificateLocator;

        private readonly IQueries<CertificateContainer> certificateContainerQueries;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateRetrieverFromMongo"/> class.
        /// </summary>
        /// <param name="environment">Environment executing against.</param>
        /// <param name="encryptingCertificateLocator">Locator for the certificate installed on computer to use to decrypt password.</param>
        /// <param name="certificateContainerQueries">Query interface for retrieving the certificates.</param>
        public CertificateRetrieverFromMongo(string environment, CertificateLocator encryptingCertificateLocator, IQueries<CertificateContainer> certificateContainerQueries)
        {
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }

            if (certificateContainerQueries == null)
            {
                throw new ArgumentNullException("certificateContainerQueries");
            }

            if (encryptingCertificateLocator == null)
            {
                throw new ArgumentNullException("encryptingCertificateLocator");
            }

            this.environment = environment;
            this.encryptingCertificateLocator = encryptingCertificateLocator;
            this.certificateContainerQueries = certificateContainerQueries;
        }

        /// <inheritdoc />
        public CertificateFile GetCertificateByName(string name)
        {
            lock (this.fileSync)
            {
                var getOneAsync = this.certificateContainerQueries.GetOneAsync(_ => _.Environment == this.environment);
                getOneAsync.Wait();
                var certificateContainer = getOneAsync.Result;

                var certificateDetails =
                    certificateContainer.Certificates.SingleOrDefault(
                        _ => string.Equals(_.Name, name, StringComparison.CurrentCultureIgnoreCase));

                Func<string, SecureString> stringDecryptor = encryptedInput =>
                    {
                        var decryptedText = Encryptor.Decrypt(encryptedInput, this.encryptingCertificateLocator);
                        return MachineManager.ConvertStringToSecureString(decryptedText);
                    };

                var certDetails = certificateDetails == null ? null : certificateDetails.ToCertificateDetails(stringDecryptor);
                return certDetails;
            }
        }
    }
}