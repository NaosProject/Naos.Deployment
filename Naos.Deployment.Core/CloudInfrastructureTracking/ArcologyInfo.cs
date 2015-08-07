// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArcologyInfo.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.CloudInfrastructureTracking
{
    using System.Collections.Generic;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Container with the description of an arcology (defines things needed to add to the arcology).
    /// </summary>
    public class ArcologyInfo
    {
        /// <summary>
        /// Gets or sets the list of supported cloud containers in the arcology.
        /// </summary>
        public ICollection<CloudContainerDescription> CloudContainers { get; set; }

        /// <summary>
        /// Gets or sets the list of supported hosting ID's.
        /// </summary>
        public IDictionary<string, string> RootDomainHostingIdMap { get; set; }

        /// <summary>
        /// Gets or sets the location of the arcology.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the map of Windows SKU's to image search patterns.
        /// </summary>
        public IDictionary<WindowsSku, string> WindowsSkuSearchPatternMap { get; set; }
    }
}