// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IShareFilePath.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Interface to support sharing a file path between handlers and future messages.
    /// </summary>
    public interface IShareFilePath : IShare
    {
        /// <summary>
        /// Gets or sets the file path to share.
        /// </summary>
        string FilePath { get; set; }
    }
}
