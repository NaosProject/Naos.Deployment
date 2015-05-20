// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManageCertificates.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Security;

    /// <summary>
    /// Interface to retrieving certificates and their associated passwords.
    /// </summary>
    public interface IManageCertificates
    {
        /// <summary>
        /// Gets the bytes of a certificate by name.
        /// </summary>
        /// <param name="certificateName">Name of the certificate in question.</param>
        /// <returns>Bytes of the certificate file.</returns>
        byte[] GetCertificateBytes(string certificateName);

        /// <summary>
        /// Gets the password of a certificate by name.
        /// </summary>
        /// <param name="certificateName">Name of the certificate in question.</param>
        /// <returns>Password of the certificate (as a SecureString).</returns>
        SecureString GetCertificatePassword(string certificateName);

        /// <summary>
        /// Gets the file name of a certificate by its name.
        /// </summary>
        /// <param name="certificateName">Name of the certificate in question.</param>
        /// <returns>Filename that is appropriate for the certificate.</returns>
        string GetCertificateFileName(string certificateName);
    }
}
