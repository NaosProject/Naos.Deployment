// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IShareFileLocation.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using Naos.FileJanitor.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Interface to support sharing a file location between handlers and future messages.
    /// </summary>
    public interface IShareFileLocation : IShare
    {
        /// <summary>
        /// Gets or sets details about a file.
        /// </summary>
        FileLocation FileLocation { get; set; }
    }
}
