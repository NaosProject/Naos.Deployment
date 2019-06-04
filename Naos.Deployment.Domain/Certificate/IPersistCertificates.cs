// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPersistCertificates.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
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
        /// <param name="certificate">Certificate to encrypt and load.</param>
        /// <returns>Task for async.</returns>
        Task PersistCertificateAsync(CertificateDescriptionWithEncryptedPfxPayload certificate);

        /// <summary>
        /// Writes encrypted certificate.
        /// </summary>
        /// <param name="certificate">Certificate to load.</param>
        /// <param name="encryptingCertificateLocator">Locator for the certificate to use to encrypt the password and bytes of the certificate.</param>
        /// <returns>Task for async.</returns>
        Task PersistCertificateAsync(CertificateDescriptionWithClearPfxPayload certificate, CertificateLocator encryptingCertificateLocator);
    }
}