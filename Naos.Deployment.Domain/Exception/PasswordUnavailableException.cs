// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PasswordUnavailableException.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception for when a password could not be retrieved for an instance because it was unavailable.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Want to control InstanceId property through constructor.")]
    [Serializable]
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
        /// Initializes a new instance of the <see cref="PasswordUnavailableException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Reading context.</param>
        protected PasswordUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.InstanceId = info.GetString(nameof(this.InstanceId));
        }

        /// <inheritdoc cref="Exception" />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(this.InstanceId), this.InstanceId);
        }

        /// <summary>
        /// Gets the ID (per the computing platform provider) of the instance the task deployed to.
        /// </summary>
        public string InstanceId { get; private set; }
    }
}