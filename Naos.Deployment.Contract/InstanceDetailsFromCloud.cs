// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceDetailsFromCloud.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    /// <summary>
    /// Container with the details of an instance taken from the cloud.
    /// </summary>
    public class InstanceDetailsFromCloud
    {
        /// <summary>
        /// Gets or sets the ID (per the cloud provider) of the instance.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the instance.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the location (per the cloud provider) of the instance.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets a property bag of system specific details.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the state (per the cloud provider) of the instance.
        /// </summary>
        public string InstanceState { get; set; }
    }
}