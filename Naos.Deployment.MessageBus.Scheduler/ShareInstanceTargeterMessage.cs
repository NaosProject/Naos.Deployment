﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareInstanceTargeterMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Scheduler
{
    using Naos.Deployment.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message to share an instance targeter.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeter", Justification = "Spelling/name is correct.")]
    public class ShareInstanceTargeterMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the instance targeter to share with other messages in the sequence.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeters", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Shares need to be arrays.")]
        public InstanceTargeterBase[] InstanceTargetersToShare { get; set; }
    }
}
