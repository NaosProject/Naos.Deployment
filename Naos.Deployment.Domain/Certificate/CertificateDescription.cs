// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateDescription.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;
    using System.ComponentModel;

    using OBeautifulCode.DateTime;
    using OBeautifulCode.Validation.Recipes;

    /// <summary>
    /// Model object to hold necessary information to inflate a certificate on a machine.
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public abstract class CertificateDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateDescription"/> class.
        /// </summary>
        /// <param name="friendlyName">Friendly name of the certificate (does NOT have to match the "Common Name").</param>
        /// <param name="thumbprint">Thumbprint of the certificate.</param>
        /// <param name="validityWindowInUtc">Date range that the certificate is valid.</param>
        /// <param name="certificateAttributes">Attributes of the certificate.</param>
        /// <param name="certificateSigningRequestPemEncoded">Optional PEM Encoded certificate signing request (default will be NULL).</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pem", Justification = "Spelling/name is correct.")]
        protected CertificateDescription(string friendlyName, string thumbprint, DateTimeRangeInclusive validityWindowInUtc, Dictionary<string, string> certificateAttributes, string certificateSigningRequestPemEncoded = null)
        {
            new { friendlyName }.Must().NotBeNullNorWhiteSpace();
            new { thumbprint }.Must().NotBeNullNorWhiteSpace();
            new { validityWindowInUtc }.Must().NotBeNull();
            new { certificateAttributes }.Must().NotBeNull();

            this.FriendlyName = friendlyName;
            this.Thumbprint = thumbprint;
            this.ValidityWindowInUtc = validityWindowInUtc;
            this.CertificateAttributes = certificateAttributes;
            this.CertificateSigningRequestPemEncoded = certificateSigningRequestPemEncoded;
        }

        /// <summary>
        /// Gets the friendly name of the certificate (does NOT have to match the "Common Name").
        /// </summary>
        public string FriendlyName { get; private set; }

        /// <summary>
        /// Gets the thumbprint of the certificate.
        /// </summary>
        public string Thumbprint { get; private set; }

        /// <summary>
        /// Gets the date range that the certificate is valid.
        /// </summary>
        public DateTimeRangeInclusive ValidityWindowInUtc { get; private set; }

        /// <summary>
        /// Gets the attributes of the certificate.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Needs to be a dictionary for mongo to serialize correctly...")]
        public Dictionary<string, string> CertificateAttributes { get; private set; }

        /// <summary>
        /// Gets the optional PEM Encoded certificate signing request (default will be NULL).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pem", Justification = "Spelling/name is correct.")]
        public string CertificateSigningRequestPemEncoded { get; private set; }

        /// <summary>
        /// Generates a file to write the bytes to.
        /// </summary>
        /// <returns>File name to use for the certificate.</returns>
        public string GenerateFileName()
        {
            return this.FriendlyName + ".pfx";
        }

        /// <summary>
        /// Gets the thumbprint cleaned up for use Powershell paths.
        /// </summary>
        /// <returns>Cleanup up thumbpring for path use.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Want a method.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Powershell", Justification = "Spelled correctly.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pathable", Justification = "Spelled correctly.")]
        public string GetPowershellPathableThumbprint()
        {
            return this.Thumbprint.Replace(" ", string.Empty);
        }
    }
}
