// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArcologyInfoContainer.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Persistence
{
    using Naos.Deployment.Domain;

    /// <summary>
    /// Container object to hold an arcology info and save it in Mongo.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arcology", Justification = "Spelling/name is correct.")]
    public class ArcologyInfoContainer
    {
        /// <summary>
        /// Gets or sets the ID of the record.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the arcology info.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arcology", Justification = "Spelling/name is correct.")]
        public ArcologyInfo ArcologyInfo { get; set; }
    }
}
