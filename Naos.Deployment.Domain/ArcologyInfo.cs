// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArcologyInfo.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;

    using OBeautifulCode.Assertion.Recipes;

    /// <summary>
    /// Container with the description of an arcology (defines things needed to add to the arcology).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arcology", Justification = "Spelling/name is correct.")]
    public class ArcologyInfo
    {
        /// <summary>
        /// Gets or sets the location of the arcology.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the serialized environment specification.
        /// </summary>
        public string SerializedEnvironmentSpecification { get; set; }

        /// <summary>
        /// Gets or sets the list of supported computing containers in the arcology.
        /// </summary>
        public IReadOnlyCollection<ComputingContainerDescription> ComputingContainers { get; set; }

        /// <summary>
        /// Gets or sets the list of supported hosting ID's.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Needs to be a dictionary for mongo to serialize correctly...")]
        public Dictionary<string, string> RootDomainHostingIdMap { get; set; }

        /// <summary>
        /// Gets or sets the map of Windows SKU's to image search patterns.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sku", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Needs to be a dictionary for mongo to serialize correctly...")]
        public Dictionary<WindowsSku, string> WindowsSkuSearchPatternMap { get; set; }

        /// <summary>
        /// Checks an IP Address against a CIDR range.
        /// </summary>
        /// <example>bool result = IsInRange("10.50.30.7", "10.0.0.0/8");.</example>
        /// <param name="ipAddress">IP Address to test.</param>
        /// <param name="cidr">CIDR mask to use as range definition.</param>
        /// <returns>Value indicating whether the provided IP was in the specified range.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "cidr", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
#pragma warning disable SA1305 // Field names should not use Hungarian notation
        public static bool IsIpAddressInRange(string ipAddress, string cidr)
#pragma warning restore SA1305 // Field names should not use Hungarian notation
        {
            new { ipAddress }.AsArg().Must().NotBeNullNorWhiteSpace();
            new { cidr }.AsArg().Must().NotBeNullNorWhiteSpace();

            if (cidr == "0.0.0.0/0")
            {
                return true;
            }

            string[] parts = cidr.Split('/');

            var addressNumeric = BitConverter.ToInt32(IPAddress.Parse(parts[0]).GetAddressBytes(), 0);
            var cidrAddressNumeric = BitConverter.ToInt32(IPAddress.Parse(ipAddress).GetAddressBytes(), 0);
            var cidrNumericMask = IPAddress.HostToNetworkOrder(-1 << (32 - int.Parse(parts[1], CultureInfo.InvariantCulture)));

            return (addressNumeric & cidrNumericMask) == (cidrAddressNumeric & cidrNumericMask);
        }
    }
}
