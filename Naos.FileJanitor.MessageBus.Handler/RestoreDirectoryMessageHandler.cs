// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestoreDirectoryMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Handler
{
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;

    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.FileJanitor.S3;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.Validation.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Message handler for <see cref="RestoreDirectoryMessage" />.
    /// </summary>
    public class RestoreDirectoryMessageHandler : MessageHandlerBase<RestoreDirectoryMessage>, IShareFilePath
    {
        /// <inheritdoc />
        public override async Task HandleAsync(RestoreDirectoryMessage message)
        {
            new { message }.Must().NotBeNull();

            var filePath = message.FilePath;
            var userDefinedMetadata = message.UserDefinedMetadata;
            var targetFilePath = message.TargetFilePath;

            using (var log = Log.Enter(() => new { Message = message, FilePath = filePath }))
            {
                await FileExchanger.RestoreDownload(filePath, targetFilePath, userDefinedMetadata);

                // share restored directory
                this.FilePath = await Task.FromResult(targetFilePath);

                log.Trace(() => Invariant($"Restored directory to {targetFilePath}."));
            }
        }

        /// <inheritdoc />
        public string FilePath { get; set; }
    }
}
