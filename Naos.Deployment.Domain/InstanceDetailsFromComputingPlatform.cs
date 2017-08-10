// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceDetailsFromComputingPlatform.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Container with the details of an instance taken from the hosting platform.
    /// </summary>
    public class InstanceDetailsFromComputingPlatform
    {
        /// <summary>
        /// Gets or sets the ID (per the computing platform provider) of the instance.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the instance.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the location (per the computing platform provider) of the instance.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets a property bag of system specific details.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the status of the instance.
        /// </summary>
        public InstanceStatus InstanceStatus { get; set; }

        /// <summary>
        /// Gets or sets the private IP address.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Name I want.")]
        public string PrivateIpAddress { get; set; }
    }
}