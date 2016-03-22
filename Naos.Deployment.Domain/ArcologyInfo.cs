// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArcologyInfo.cs" company="Naos">
//   Copyright 2015 Naos
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
        /// Gets or sets the list of supported computing containers in the arcology.
        /// </summary>
        public IReadOnlyCollection<ComputingContainerDescription> ComputingContainers { get; set; }

        /// <summary>
        /// Gets or sets the list of supported hosting ID's.
        /// </summary>
        public IReadOnlyDictionary<string, string> RootDomainHostingIdMap { get; set; }

        /// <summary>
        /// Gets or sets the location of the arcology.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the map of Windows SKU's to image search patterns.
        /// </summary>
        public IReadOnlyDictionary<WindowsSku, string> WindowsSkuSearchPatternMap { get; set; }
    }
}