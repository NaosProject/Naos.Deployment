// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceDetailsFromComputingPlatform.cs" company="Naos">
//   Copyright 2015 Naos
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
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the status of the instance.
        /// </summary>
        public InstanceStatus InstanceStatus { get; set; }
    }
}