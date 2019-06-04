// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeployedInstance.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Container object for storing instances in tracking.
    /// </summary>
    public class DeployedInstance
    {
        /// <summary>
        /// Gets or sets the related instance description.
        /// </summary>
        public InstanceDescription InstanceDescription { get; set; }

        /// <summary>
        /// Gets or sets the related instance details.
        /// </summary>
        public InstanceCreationDetails InstanceCreationDetails { get; set; }

        /// <summary>
        /// Gets or sets the related deployment configuration.
        /// </summary>
        public DeploymentConfiguration DeploymentConfig { get; set; }
    }
}