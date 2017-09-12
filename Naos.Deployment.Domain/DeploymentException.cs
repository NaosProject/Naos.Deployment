// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentException.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception occurred trying to do a deployment.
    /// </summary>
    [Serializable]
    public class DeploymentException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentException"/> class.
        /// </summary>
        public DeploymentException()
        {
        }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Reading context.</param>
        protected DeploymentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}