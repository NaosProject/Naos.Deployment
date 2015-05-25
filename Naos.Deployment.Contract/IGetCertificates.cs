// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IGetCertificates.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
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
        CertificateDetails GetCertificateByName(string name);
    }
}
