// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPersistCertificates.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for getting certificate information by name.
    /// </summary>
    public interface IPersistCertificates
    {
        /// <summary>
        /// Encrypts and writes certificate.
        /// </summary>
        /// <param name="certificateToLoad">Certificate to encrypt and load.</param>
        /// <returns>Task for async.</returns>
        Task PersistCertficateAsync(CertificateDescriptionWithEncryptedPfxPayload certificateToLoad);

        /// <summary>
        /// Writes encrypted certificate.
        /// </summary>
        /// <param name="certificate">Certificate to load.</param>
        /// <param name="encryptingCertificateLocator">Locator for the certificate to use to encrypt the password and bytes of the certificate.</param>
        /// <returns>Task for async.</returns>
        Task PersistCertficateAsync(CertificateDescriptionWithClearPfxPayload certificate, CertificateLocator encryptingCertificateLocator);
    }
}