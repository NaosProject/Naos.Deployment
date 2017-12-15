// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullCertificateRetriever.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Null object implementation for testing.
    /// </summary>
    public class NullCertificateRetriever : IGetCertificates
    {
        /// <inheritdoc />
        public async Task<CertificateDescriptionWithClearPfxPayload> GetCertificateByNameAsync(string name)
        {
            return await Task.FromResult((CertificateDescriptionWithClearPfxPayload)null);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<string>> GetAllCertificateNamesAsync()
        {
            return await Task.FromResult((IReadOnlyCollection<string>)new string[0]);
        }
    }
}
