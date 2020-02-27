// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageDetails.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Details needed for looking up images.
    /// </summary>
    public class ImageDetails
    {
        /// <summary>
        /// Gets or sets the alias of the owner to allow images from.
        /// </summary>
        public string OwnerAlias { get; set; }

        /// <summary>
        /// Gets or sets the search pattern to find the correct image.
        /// </summary>
        public string SearchPattern { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to throw on multiple matches.
        /// </summary>
        public bool ShouldHaveSingleMatch { get; set; }

        /// <summary>
        /// Gets or sets a specific system ID (will override search logic).
        /// </summary>
        public string ImageSystemId { get; set; }
    }
}
