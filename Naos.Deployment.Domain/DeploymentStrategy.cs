// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentStrategy.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    using OBeautifulCode.Equality.Recipes;

    /// <summary>
    /// Information about how the deployment should run.
    /// </summary>
    public class DeploymentStrategy : IEquatable<DeploymentStrategy>
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to use the initialization script on launch.
        /// </summary>
        public bool IncludeInstanceInitializationScript { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to run setup steps (instance level OR initialization specific).
        /// </summary>
        public bool RunSetupSteps { get; set; }

        /// <summary>
        /// Equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if equal.</returns>
        public static bool operator ==(DeploymentStrategy first, DeploymentStrategy second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return (first.IncludeInstanceInitializationScript == second.IncludeInstanceInitializationScript) && (first.RunSetupSteps == second.RunSetupSteps);
        }

        /// <summary>
        /// Not equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if not equal.</returns>
        public static bool operator !=(DeploymentStrategy first, DeploymentStrategy second) => !(first == second);

        /// <inheritdoc />
        public bool Equals(DeploymentStrategy other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as DeploymentStrategy);

        /// <inheritdoc />
        public override int GetHashCode() => HashCodeHelper.Initialize().Hash(this.IncludeInstanceInitializationScript).Hash(this.RunSetupSteps).Value;
    }
}
