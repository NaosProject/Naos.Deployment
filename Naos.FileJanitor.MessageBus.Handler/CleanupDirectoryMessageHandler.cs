// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CleanupDirectoryMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Handler
{
    using System.Threading.Tasks;

    using Naos.FileJanitor.Domain;
    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.Validation.Recipes;

    /// <summary>
    /// Handler to handle CleanupDirectoryMessages.
    /// </summary>
    public class CleanupDirectoryMessageHandler : MessageHandlerBase<CleanupDirectoryMessage>
    {
        /// <inheritdoc />
        public override async Task HandleAsync(CleanupDirectoryMessage message)
        {
            new { message }.Must().NotBeNull();

            var directoryFullPath = message.DirectoryFullPath;
            var recursive = message.Recursive;
            var retentionWindow = message.RetentionWindow;
            var deleteEmptyDirectories = message.DeleteEmptyDirectories;
            var dateRetrievalStrategy = message.FileDateRetrievalStrategy;

            await Task.Run(() => FilePathJanitor.Cleanup(directoryFullPath, retentionWindow, recursive, deleteEmptyDirectories, dateRetrievalStrategy));
        }
    }
}