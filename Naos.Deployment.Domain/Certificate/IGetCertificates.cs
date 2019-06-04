// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IGetCertificates.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Want a method for async.")]
        Task<IReadOnlyCollection<string>> GetAllCertificateNamesAsync();
    }
}
