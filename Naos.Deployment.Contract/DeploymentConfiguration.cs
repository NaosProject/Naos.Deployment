// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfiguration.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    /// <summary>
    /// Model object with necessary details to deploy software to a machine.
    /// </summary>
    public class DeploymentConfiguration
    {
        /// <summary>
        /// Gets or sets the type of instance to deploy to.
        /// </summary>
        public string InstanceType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this needs to be accessible publicly.
        /// </summary>
        public bool? IsPubliclyAccessible { get; set; }

        /// <summary>
        /// Gets or sets the volumes to add to the instance.
        /// </summary>
        public ICollection<Volume> Volumes { get; set; }

        /// <summary>
        /// Gets or sets the initialization strategies of the deployment.
        /// </summary>
        public ICollection<InitializationStrategy> InitializationStrategies { get; set; }
    }
}
