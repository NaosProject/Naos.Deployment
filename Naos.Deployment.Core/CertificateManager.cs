// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Security;
    using System.Text;

    using Naos.Deployment.Contract;
    using Naos.WinRM;

    /// <inheritdoc />
    public class CertificateManager : IManageCertificates
    {
        /// <inheritdoc />
        public byte[] GetCertificateBytes(string certificateName)
        {
            var base64bytes = @"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";
            return Convert.FromBase64String(base64bytes);
        }

        /// <inheritdoc />
        public SecureString GetCertificatePassword(string certificateName)
        {
            return MachineManager.ConvertStringToSecureString("adSDf0D9)fe#;f");
        }

        /// <inheritdoc />
        public string GetCertificateFileName(string certificateName)
        {
            return certificateName + ".pfx";
        }
    }
}
