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

    /// <summary>
    /// Implementation using Mongo of <see cref="IGetCertificates"/>.
    /// </summary>
    public class CertificateRetrieverFromMongo : IGetCertificates
    {
        private readonly object fileSync = new object();

        private readonly string environment;

        private readonly DeploymentDatabase database;

        private readonly CertificateLocator encryptingCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateRetrieverFromMongo"/> class.
        /// </summary>
        /// <param name="environment">Environment executing against.</param>
        /// <param name="database">Database to read certificates from.</param>
        /// <param name="encryptingCertificate">Locator for the certificate installed on computer to use to decrypt password.</param>
        public CertificateRetrieverFromMongo(string environment, DeploymentDatabase database, CertificateLocator encryptingCertificate)
        {
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }

            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            if (encryptingCertificate == null)
            {
                throw new ArgumentNullException("encryptingCertificate");
            }

            this.environment = environment;
            this.database = database;
            this.encryptingCertificate = encryptingCertificate;
        }

        /// <inheritdoc />
        public CertificateFile GetCertificateByName(string name)
        {
            lock (this.fileSync)
            {
                var queries = this.database.GetQueriesInterface<CertificateContainer>();
                var getOneAsync = queries.GetOneAsync(_ => _.Environment == this.environment);
                getOneAsync.Wait();
                var certificateContainer = getOneAsync.Result;

                var certificateDetails =
                    certificateContainer.Certificates.SingleOrDefault(
                        _ => string.Equals(_.Name, name, StringComparison.CurrentCultureIgnoreCase));

                Func<string, SecureString> stringDecryptor = encryptedInput =>
                    {
                        var decryptedText = Encryptor.Decrypt(encryptedInput, this.encryptingCertificate);
                        return MachineManager.ConvertStringToSecureString(decryptedText);
                    };

                var certDetails = certificateDetails == null ? null : certificateDetails.ToCertificateDetails(stringDecryptor);
                return certDetails;
            }
        }
    }
}