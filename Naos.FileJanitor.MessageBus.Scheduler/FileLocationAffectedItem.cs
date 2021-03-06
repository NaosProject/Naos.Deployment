﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileLocationAffectedItem.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using Naos.FileJanitor.Domain;
    using Naos.MessageBus.Domain;
    using OBeautifulCode.Representation.System;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Type;

    /// <summary>
    /// Model object to hold an affected item from .
    /// </summary>
    public class FileLocationAffectedItem
    {
        /// <summary>
        /// Serialization description to use for saving affected file locations into affected items list.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Want this to be a read only field.")]
        public static readonly SerializerRepresentation ItemSerializerRepresentation =
            new SerializerRepresentation(
                SerializationKind.Json,
                typeof(FileJanitorMessageBusJsonSerializationConfiguration).ToRepresentation());

        /// <summary>
        /// Gets or sets a message about the event.
        /// </summary>
        public string FileLocationAffectedItemMessage { get; set; }

        /// <summary>
        /// Gets or sets the source or target location.
        /// </summary>
        public FileLocation FileLocation { get; set; }

        /// <summary>
        /// Gets or sets the source or target path.
        /// </summary>
        public string FilePath { get; set; }
    }
}
