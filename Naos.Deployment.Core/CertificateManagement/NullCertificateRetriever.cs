// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullCertificateRetriever.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using Naos.Deployment.Contract;

    /// <summary>
    /// Null object implementation for testing.
    /// </summary>
    public class NullCertificateRetriever : IGetCertificates
    {
        /// <inheritdoc />
        public CertificateFile GetCertificateByName(string name)
        {
            return new CertificateFile();
        }
    }
}
