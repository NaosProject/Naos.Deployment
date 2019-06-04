// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeleteFileMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Handler
{
    using System.IO;
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;

    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Message handler to delete a file.
    /// </summary>
    public class DeleteFileMessageHandler : MessageHandlerBase<DeleteFileMessage>, IShareFilePath
    {
        /// <inheritdoc />
        public override async Task HandleAsync(DeleteFileMessage message)
        {
            using (var log = Log.Enter(() => new { Message = message, message.FilePath }))
            {
                if (message.FilePath == null || !File.Exists(message.FilePath))
                {
                    throw new FileNotFoundException(
                        "Could not find specified filepath: " + (message.FilePath ?? "[NULL]"));
                }

                this.FilePath = message.FilePath;

                log.Trace(() => "Start deleting file.");
                await Task.Run(() => File.Delete(message.FilePath));
                log.Trace(() => "Finished deleting file.");
            }
        }

        /// <inheritdoc />
        public string FilePath { get; set; }
    }
}
