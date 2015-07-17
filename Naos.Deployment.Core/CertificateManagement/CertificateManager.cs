// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CertificateManagement
{
    using System;
    using System.Collections.Generic;

    using Naos.Deployment.Contract;
    using Naos.WinRM;

    /// <summary>
    /// Class to be used to marshal available certificate information.
    /// </summary>
    public class CertificateManager
    {
        /// <summary>
        /// Gets or sets the certificates.
        /// </summary>
        public List<CertificateContainer> Certificates { get; set; }
    }

    /// <summary>
    /// Class to allow TheSafe to be serialized and deserialized but still provide a CertificateDetails object.
    /// </summary>
    public class CertificateContainer
    {
        /// <summary>
        /// Converts the container to a certificate details class.
        /// </summary>
        /// <returns>Converted details version.</returns>
        public CertificateDetails ToCertificateDetails()
        {
            var ret = new CertificateDetails
                          {
                              Name = this.Name,
                              CertificatePassword =
                                  MachineManager.ConvertStringToSecureString(this.Password),
                              FileBytes = Convert.FromBase64String(this.Base64Bytes),
                          };

            return ret;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the bytes in Base64 format.
        /// </summary>
        public string Base64Bytes { get; set; }
    }
}