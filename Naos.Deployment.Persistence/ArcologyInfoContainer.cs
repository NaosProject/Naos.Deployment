// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArcologyInfoContainer.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Persistence
{
    using Naos.Deployment.Domain;

    /// <summary>
    /// Container object to hold an instance and save it in Mongo.
    /// </summary>
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
        public ArcologyInfo ArcologyInfo { get; set; }
    }
}
