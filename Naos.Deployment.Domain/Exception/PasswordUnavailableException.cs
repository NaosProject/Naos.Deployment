// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PasswordUnavailableException.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    /// <summary>
    /// Exception for when a password could not be retrieved for an instance because it was unavailable.
    /// </summary>
    public class PasswordUnavailableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordUnavailableException"/> class.
        /// </summary>
        /// <param name="instanceId">Instance ID that was attempted to retrieve a password for.</param>
        /// <param name="message">Message of the exception.</param>
        public PasswordUnavailableException(string instanceId, string message)
            : base(message)
        {
            this.InstanceId = instanceId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordUnavailableException"/> class.
        /// </summary>
        /// <param name="instanceId">Instance ID that was attempted to retrieve a password for.</param>
        /// <param name="message">Message of the exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public PasswordUnavailableException(string instanceId, string message, Exception innerException)
            : base(message, innerException)
        {
            this.InstanceId = instanceId;
        }

        /// <summary>
        /// Gets the ID (per the computing platform provider) of the instance the task deployed to.
        /// </summary>
        public string InstanceId { get; private set; }
    }
}