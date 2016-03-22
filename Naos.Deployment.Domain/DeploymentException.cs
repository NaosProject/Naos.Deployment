// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentException.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    /// <summary>
    /// Exception occurred trying to do a deployment.
    /// </summary>
    [Serializable]
    public class DeploymentException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public DeploymentException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public DeploymentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}