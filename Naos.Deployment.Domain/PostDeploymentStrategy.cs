// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PostDeploymentStrategy.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    using OBeautifulCode.Math;

    /// <summary>
    /// Information about what to do after the deployment has run.
    /// </summary>
    public class PostDeploymentStrategy : IEquatable<PostDeploymentStrategy>
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to shutdown the instance after deployment (otherwise it will be left running).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TurnOff", Justification = "Spelling/name is correct.")]
        public bool TurnOffInstance { get; set; }

        /// <summary>
        /// Equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if equal.</returns>
        public static bool operator ==(PostDeploymentStrategy first, PostDeploymentStrategy second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
            {
                return false;
            }

            return first.TurnOffInstance == second.TurnOffInstance;
        }

        /// <summary>
        /// Not equal operator.
        /// </summary>
        /// <param name="first">Left item.</param>
        /// <param name="second">Right item.</param>
        /// <returns>Value indicating if not equal.</returns>
        public static bool operator !=(PostDeploymentStrategy first, PostDeploymentStrategy second) => !(first == second);

        /// <inheritdoc />
        public bool Equals(PostDeploymentStrategy other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object obj) => this == (obj as PostDeploymentStrategy);

        /// <inheritdoc />
        public override int GetHashCode() => HashCodeHelper.Initialize().Hash(this.TurnOffInstance).Value;
    }
}