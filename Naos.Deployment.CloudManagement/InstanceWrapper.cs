// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceWrapper.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.CloudManagement
{
    using Naos.Deployment.Contract;

    /// <summary>
    /// Container object for storing instances in tracking.
    /// </summary>
    public class InstanceWrapper
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