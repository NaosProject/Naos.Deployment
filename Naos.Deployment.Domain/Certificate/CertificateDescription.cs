// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateDescription.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    using Spritely.Recipes;

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
        /// <param name="validityWindowInUtc">Date range that the certificate is valid.</param>
        /// <param name="certificateAttributes">Attributes of the certificate.</param>
        /// <param name="certificateSigningRequestPemEncoded">Optional PEM Encoded certificate signing request (default will be NULL).</param>
        protected CertificateDescription(string friendlyName, DateTimeRange validityWindowInUtc, Dictionary<string, string> certificateAttributes, string certificateSigningRequestPemEncoded = null)
        {
            new { friendlyName }.Must().NotBeNull().And().NotBeWhiteSpace().OrThrowFirstFailure();
            new { validityWindowInUtc, certificateAttributes }.Must().NotBeNull().OrThrowFirstFailure();

            this.FriendlyName = friendlyName;
            this.ValidityWindowInUtc = validityWindowInUtc;
            this.CertificateAttributes = certificateAttributes;
            this.CertificateSigningRequestPemEncoded = certificateSigningRequestPemEncoded;
        }

        /// <summary>
        /// Gets the friendly name of the certificate (does NOT have to match the "Common Name").
        /// </summary>
        public string FriendlyName { get; private set; }

        /// <summary>
        /// Gets the date range that the certificate is valid.
        /// </summary>
        public DateTimeRange ValidityWindowInUtc { get; private set; }

        /// <summary>
        /// Gets the attributes of the certificate.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Needs to be a dictionary for mongo to serialize correctly...")]
        public Dictionary<string, string> CertificateAttributes { get; private set; }

        /// <summary>
        /// Gets the optional PEM Encoded certificate signing request (default will be NULL).
        /// </summary>
        public string CertificateSigningRequestPemEncoded { get; private set; }

        /// <summary>
        /// Generates a file to write the bytes to.
        /// </summary>
        /// <returns>File name to use for the certificate.</returns>
        public string GenerateFileName()
        {
            return this.FriendlyName + ".pfx";
        }
    }

    /// <summary>
    /// Placeholder date time range class.
    /// </summary>
    public class DateTimeRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeRange"/> class.
        /// </summary>
        /// <param name="start">Start date time.</param>
        /// <param name="end">End date time.</param>
        public DateTimeRange(DateTime start, DateTime end)
        {
            this.Start = start;
            this.End = end;
        }

        /// <summary>
        /// Gets the start date time.
        /// </summary>
        public DateTime Start { get; private set; }

        /// <summary>
        /// Gets the end date time.
        /// </summary>
        public DateTime End { get; private set; }
    }
}
