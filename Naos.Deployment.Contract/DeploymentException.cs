// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentException.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
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
    }
}