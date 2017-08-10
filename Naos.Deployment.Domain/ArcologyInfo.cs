// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArcologyInfo.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Container with the description of an arcology (defines things needed to add to the arcology).
    /// </summary>
    public class ArcologyInfo
    {
        /// <summary>
        /// Gets or sets the location of the arcology.
        /// </summary>
        public string Location { get; set; }

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Needs to be a dictionary for mongo to serialize correctly...")]
        public Dictionary<WindowsSku, string> WindowsSkuSearchPatternMap { get; set; }
    }
}