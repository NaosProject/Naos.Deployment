// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FindFileMessage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using Naos.FileJanitor.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message object to find a file in storage using provided criteria and yield a <see cref="FileLocation"/> object.
    /// </summary>
    public class FindFileMessage : IMessage
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the storage location.
        /// </summary>
        public string ContainerLocation { get; set; }

        /// <summary>
        /// Gets or sets the storage container.
        /// </summary>
        public string Container { get; set; }

        /// <summary>
        /// Gets or sets the search pattern for the key (multiples will be handled according to the strategy).
        /// </summary>
        public string KeyPrefixSearchPattern { get; set; }

        /// <summary>
        /// Gets or sets the strategy to use when multiple keys are found (default is throw on multiples).
        /// </summary>
        public MultipleKeysFoundStrategy MultipleKeysFoundStrategy { get; set; }
    }
}
