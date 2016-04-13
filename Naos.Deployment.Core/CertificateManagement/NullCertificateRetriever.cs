// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullCertificateRetriever.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Null object implementation for testing.
    /// </summary>
    public class NullCertificateRetriever : IGetCertificates
    {
        /// <inheritdoc />
        public Task<CertificateFile> GetCertificateByNameAsync(string name)
        {
            return Task.FromResult(new CertificateFile());
        }
    }
}
