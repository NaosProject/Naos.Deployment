// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IGetCertificates.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for getting certificate information by name.
    /// </summary>
    public interface IGetCertificates
    {
        /// <summary>
        /// Gets certificate details by certificate name (null if not found).
        /// </summary>
        /// <param name="name">Name of the certificate to find.</param>
        /// <returns>Certificate details matching name; null if not found.</returns>
        Task<CertificateDescriptionWithClearPfxPayload> GetCertificateByNameAsync(string name);
    }
}
