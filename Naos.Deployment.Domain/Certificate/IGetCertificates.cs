// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IGetCertificates.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;
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

        /// <summary>
        /// Gets all of the certificate names in the store.
        /// </summary>
        /// <returns>List of the certificate names in the store.</returns>
        Task<IReadOnlyCollection<string>> GetAllCertificateNamesAsync();
    }
}
