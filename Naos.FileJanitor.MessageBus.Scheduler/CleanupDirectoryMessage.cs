// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CleanupDirectoryMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using System;

    using Naos.FileJanitor.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message to describe a directory to get cleaned up per the specified policies.
    /// </summary>
    public class CleanupDirectoryMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the full path to the directory to clean up.
        /// </summary>
        public string DirectoryFullPath { get; set; }

        /// <summary>
        /// Gets or sets the timespan of the threshold before deleting the file.
        /// </summary>
        public TimeSpan RetentionWindow { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to evaluate child folders or not.
        /// </summary>
        public bool Recursive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to remove empty directories found.
        /// </summary>
        public bool DeleteEmptyDirectories { get; set; }

        /// <summary>
        /// Gets or sets the strategy to use when getting the file's date.
        /// </summary>
        public DateRetrievalStrategy FileDateRetrievalStrategy { get; set; }
    }
}
