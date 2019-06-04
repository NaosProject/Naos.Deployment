// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AbortIfNoNewFileLocationForTopicMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using Naos.FileJanitor.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message to abort if a provided file location is the same as the one in the affected items.
    /// </summary>
    public class AbortIfNoNewFileLocationForTopicMessage : IMessage, IShareFileLocation, IShareTopicStatusReports
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the topic to check.
        /// </summary>
        public NamedTopic TopicToCheckAffectedItemsFor { get; set; }

        /// <inheritdoc />
        public FileLocation FileLocation { get; set; }

        /// <inheritdoc />
        public TopicStatusReport[] TopicStatusReports { get; set; }
    }
}
