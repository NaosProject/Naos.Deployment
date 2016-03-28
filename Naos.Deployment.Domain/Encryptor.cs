// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Encryptor.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;

    using Its.Configuration;

    /// <summary>
    /// Class to encrypt and decrypt text.
    /// </summary>
    public static class Encryptor
    {
        /// <summary>
        /// Encrypts input using a certificate found on the local computer.
        /// </summary>
        /// <param name="input">Input to encrypt.</param>
        /// <param name="encryptingCertificate">Certificate locator of certificate to for encryption.</param>
        /// <returns>Encrypted text.</returns>
        public static string Encrypt(string input, CertificateLocator encryptingCertificate)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (encryptingCertificate == null)
            {
                throw new ArgumentNullException("encryptingCertificate");
            }

            Func<X509Certificate2, string> funcToRunWithCertificate = certificate => input.Encrypt(certificate);
            var ret = RunWithCertificate(encryptingCertificate, funcToRunWithCertificate);
            return ret;
        }

        /// <summary>
        /// Decrypts encrypted input using a certificate found on the local computer.
        /// </summary>
        /// <param name="encryptedInput">Input that is encrypted to decrypt.</param>
        /// <param name="encryptingCertificate">Certificate locator of certificate to for encryption.</param>
        /// <returns>Decrypted text.</returns>
        public static string Decrypt(string encryptedInput, CertificateLocator encryptingCertificate)
        {
            if (encryptedInput == null)
            {
                throw new ArgumentNullException("encryptedInput");
            }

            if (encryptingCertificate == null)
            {
                throw new ArgumentNullException("encryptingCertificate");
            }

            Func<X509Certificate2, string> funcToRunWithCertificate = certificate => encryptedInput.Decrypt(certificate);
            var ret = RunWithCertificate(encryptingCertificate, funcToRunWithCertificate);
            return ret;
        }

        private static string RunWithCertificate(CertificateLocator encryptingCertificate, Func<X509Certificate2, string> funcToRunWithCertificate)
        {
            string result;
            var certificateStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                certificateStore.Open(OpenFlags.OpenExistingOnly);

                var thumbprint =
                    Regex.Replace(encryptingCertificate.CertificateThumbprint, @"[^\da-zA-z]", string.Empty)
                        .ToUpper(CultureInfo.InvariantCulture);

                var certificates = certificateStore.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    thumbprint,
                    encryptingCertificate.CertificateIsValid);

                if (certificates.Count == 0)
                {
                    throw new ArgumentException(
                        "Could not find certificate; thumbprint: " + encryptingCertificate.CertificateThumbprint);
                }

                var x509Certificate2 = certificates[0];
                result = funcToRunWithCertificate(x509Certificate2);
            }
            finally
            {
                certificateStore.Close();
            }

            return result;
        }
    }
}