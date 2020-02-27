// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Packaging.Domain;

    using OBeautifulCode.Equality.Recipes;

    /// <summary>
    /// Model object with necessary details to deploy software to a machine.
    /// </summary>
    public class DeploymentConfiguration : IEquatable<DeploymentConfiguration>
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
        public IReadOnlyCollection<Volume> Volumes { get; set; }

        /// <summary>
        /// Gets or sets the Chocolatey packages to install during the deployment.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Chocolatey", Justification = "Spelling/name is correct.")]
        public IReadOnlyCollection<PackageDescription> ChocolateyPackages { get; set; }

        /// <summary>
        /// Gets or sets the deployment strategy to describe how certain things should be handled.
        /// </summary>
        public DeploymentStrategy DeploymentStrategy { get; set; }

        /// <summary>
        /// Gets or sets the post deployment strategy to describe any steps to perform when the deployment is finished.
        /// </summary>
        public PostDeploymentStrategy PostDeploymentStrategy { get; set; }

        /// <summary>
        /// Gets or sets the map of tag name to value.
        /// </summary>
        public IReadOnlyDictionary<string, string> TagNameToValueMap { get; set; }

        private IReadOnlyCollection<KeyValuePair<string, string>> SafeSortedTags =>
            (this.TagNameToValueMap ?? new Dictionary<string, string>()).OrderBy(_ => _.Key).ToList();

        /// <summary>
        /// Equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if equal.</returns>
        public static bool operator ==(DeploymentConfiguration first, DeploymentConfiguration second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return (first.InstanceType == second.InstanceType) && (first.InstanceAccessibility == second.InstanceAccessibility)
                   && (first.InstanceCount == second.InstanceCount) && (first.Volumes ?? new Volume[0]).SequenceEqual(second.Volumes ?? new Volume[0])
                   && (first.ChocolateyPackages ?? new PackageDescription[0]).SequenceEqual(second.ChocolateyPackages ?? new PackageDescription[0])
                   && (first.DeploymentStrategy == second.DeploymentStrategy)
                   && (first.PostDeploymentStrategy == second.PostDeploymentStrategy
                   && first.SafeSortedTags.SequenceEqual(second.SafeSortedTags));
        }

        /// <summary>
        /// Not equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if not equal.</returns>
        public static bool operator !=(DeploymentConfiguration first, DeploymentConfiguration second) => !(first == second);

        /// <inheritdoc />
        public bool Equals(DeploymentConfiguration other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as DeploymentConfiguration);

        /// <inheritdoc />
        public override int GetHashCode() =>
            HashCodeHelper.Initialize()
            .Hash(this.InstanceType)
            .Hash(this.InstanceAccessibility)
            .Hash(this.InstanceCount)
            .Hash(this.Volumes)
            .Hash(this.ChocolateyPackages)
            .Hash(this.DeploymentStrategy)
            .Hash(this.PostDeploymentStrategy)
            .Hash(this.SafeSortedTags).Value;
    }
}
