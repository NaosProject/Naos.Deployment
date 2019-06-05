// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StoreFileInS3MessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Handler
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;

    using Naos.AWS.S3;
    using Naos.Configuration.Domain;
    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.FileJanitor.S3;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.Validation.Recipes;

    /// <summary>
    /// Message handler to store files in S3.
    /// </summary>
    public class StoreFileInS3MessageHandler : MessageHandlerBase<StoreFileMessage>, IShareAffectedItems
    {
        /// <inheritdoc />
        public override async Task HandleAsync(StoreFileMessage message)
        {
            if (message.FilePath == null || !File.Exists(message.FilePath))
            {
                throw new FileNotFoundException("Could not find specified filepath: " + (message.FilePath ?? "[NULL]"));
            }

            if (message.FileLocation == null)
            {
                throw new ApplicationException("Must specify file location to fetch from.");
            }

            if (string.IsNullOrEmpty(message.FileLocation.ContainerLocation))
            {
                throw new ApplicationException("Must specify region (container location).");
            }

            if (string.IsNullOrEmpty(message.FileLocation.Container))
            {
                throw new ApplicationException("Must specify bucket name (container).");
            }

            var settings = Config.Get<FileJanitorMessageHandlerSettings>();
            await this.HandleAsync(message, settings);
        }

        /// <summary>
        /// Handles a <see cref="StoreFileMessage"/>.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Needed settings to handle messages.</param>
        /// <returns>Task to support async await execution.</returns>
        public async Task HandleAsync(StoreFileMessage message, FileJanitorMessageHandlerSettings settings)
        {
            new { message }.Must().NotBeNull();
            new { settings }.Must().NotBeNull();

            var filePath = message.FilePath;
            var containerLocation = message.FileLocation.ContainerLocation;
            var container = message.FileLocation.Container;
            var key = message.FileLocation.Key;
            var uploadSecretKey = settings.UploadSecretKey;
            var uploadAccessKey = settings.UploadAccessKey;
            var hashingAlgorithmNames = message.HashingAlgorithmNames;
            var userDefinedMetadata = message.UserDefinedMetadata;

            var correlationId = Guid.NewGuid().ToString().ToUpperInvariant();

            Log.Write(() => $"Starting Store File; CorrelationId: {correlationId}, Region: {containerLocation}, BucketName: {container}, Key: {key}, FilePath: {filePath}");
            using (var log = Log.Enter(() => new { CorrelationId = correlationId }))
            {
                log.Trace(() => "Starting upload.");

                var fileManager = new FileManager(uploadAccessKey, uploadSecretKey);

                await FileExchanger.StoreFile(fileManager, filePath, containerLocation, container, key, userDefinedMetadata, hashingAlgorithmNames);

                var affectedItem = new FileLocationAffectedItem
                {
                    FileLocationAffectedItemMessage = "Stored file from path to location.",
                    FileLocation = message.FileLocation,
                    FilePath = filePath,
                };

                var serializer = this.SerializerFactory.BuildSerializer(FileLocationAffectedItem.ItemSerializationDescription);

                this.AffectedItems = new[] { new AffectedItem { Id = serializer.SerializeToString(affectedItem) } };

                log.Trace(() => "Finished upload.");
            }
        }

        /// <inheritdoc />
        public AffectedItem[] AffectedItems { get; set; }
    }
}
