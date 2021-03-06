﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopInstanceWithDelayMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Scheduler
{
    using System;
    using Naos.Deployment.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message to be processed and turn off an instance specified.
    /// </summary>
    public class StopInstanceWithDelayMessage : IMessage, IShareInstanceTargeters
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <summary>
        /// Gets or sets the minimum UTC time that must be reached before stopping.
        /// </summary>
        public DateTime MinimumDateTimeInUtcBeforeStop { get; set; }
    }
}
