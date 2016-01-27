// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfiguration.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    using Naos.Packaging.Domain;

    /// <summary>
    /// Model object with necessary details to deploy software to a machine.
    /// </summary>
    public class DeploymentConfiguration
    {
        /// <summary>
        /// Gets or sets the type of instance to deploy to.
        /// </summary>
        public InstanceType InstanceType { get; set; }

        /// <summary>
        /// Gets or sets the accessibility of the instance.
        /// </summary>
        public InstanceAccessibility InstanceAccessibility { get; set; }

        /// <summary>
        /// Gets or sets the number of instances to create with specified configuration.
        /// </summary>
        public int InstanceCount { get; set; }

        /// <summary>
        /// Gets or sets the volumes to add to the instance.
        /// </summary>
        public ICollection<Volume> Volumes { get; set; }

        /// <summary>
        /// Gets or sets the Chocolatey packages to install during the deployment.
        /// </summary>
        public ICollection<PackageDescription> ChocolateyPackages { get; set; }

        /// <summary>
        /// Gets or sets the deployment strategy to describe how certain things should be handled.
        /// </summary>
        public DeploymentStrategy DeploymentStrategy { get; set; }

        /// <summary>
        /// Gets or sets the post deployment strategy to describe any steps to perform when the deployment is finished.
        /// </summary>
        public PostDeploymentStrategy PostDeploymentStrategy { get; set; }
    }
}
