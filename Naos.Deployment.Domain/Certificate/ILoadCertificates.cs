// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILoadCertificates.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for getting certificate information by name.
    /// </summary>
    public interface ILoadCertificates
    {
        /// <summary>
        /// Encrypts and writes certificate.
        /// </summary>
        /// <param name="certificateToLoad">Certificate to encrypt and load.</param>
        /// <returns>Task for async.</returns>
        Task LoadCertficateAsync(CertificateToLoad certificateToLoad);

        /// <summary>
        /// Writes encrypted certificate.
        /// </summary>
        /// <param name="certificate">Certificate to load.</param>
        /// <returns>Task for async.</returns>
        Task LoadCertficateAsync(CertificateDetails certificate);
    }
}