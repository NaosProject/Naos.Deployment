// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FetchFileFromS3MessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Handler
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.AWS.S3;
    using Naos.Configuration.Domain;
    using Naos.FileJanitor.Domain;
    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.FileJanitor.S3;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;
    using OBeautifulCode.Serialization;
    using Spritely.Redo;

    using static System.FormattableString;

    /// <summary>
    /// Message handler to fetch a file from S3.
    /// </summary>
    public class FetchFileFromS3MessageHandler : MessageHandlerBase<FetchFileMessage>, IShareFilePath, IShareAffectedItems, IShareUserDefinedMetadata
    {
        /// <inheritdoc />
        public override async Task HandleAsync(FetchFileMessage message)
        {
            if (message.FilePath == null)
            {
                throw new FileNotFoundException("Could not use specified filepath: " + (message.FilePath ?? "[NULL]"));
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
        /// Handles a FetchFileFromS3Message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Needed settings to handle messages.</param>
        /// <returns>Task to support async await execution.</returns>
        public async Task HandleAsync(FetchFileMessage message, FileJanitorMessageHandlerSettings settings)
        {
            var correlationId = Guid.NewGuid().ToString().ToUpperInvariant();
            var containerLocation = message.FileLocation.ContainerLocation;
            var container = message.FileLocation.Container;
            var key = message.FileLocation.Key;

            Log.Write(() => Invariant($"Starting Fetch File; CorrelationId: {correlationId}, Region: {containerLocation}, BucketName: {container}, Key: {key}, RawFilePath: {message.FilePath}"));
            using (var log = Log.With(() => new { CorrelationId = correlationId }))
            {
                var fileManager = new FileManager(settings.DownloadAccessKey, settings.DownloadSecretKey);

                // shares path down because it can be augmented...
                this.FilePath = message.FilePath.Replace("{Key}", key);
                log.Write(() => $"Dowloading the file to replaced FilePath: {this.FilePath}");

                var metadata = await FileExchanger.FetchMetadata(fileManager, containerLocation, container, key);

                await FileExchanger.FetchFile(fileManager, containerLocation, container, key, this.FilePath);

                var affectedItem = new FileLocationAffectedItem
                                       {
                                           FileLocationAffectedItemMessage = "Fetched file from location to path.",
                                           FileLocation = message.FileLocation,
                                           FilePath = this.FilePath,
                                       };

                var serializer = this.SerializerFactory.BuildSerializer(FileLocationAffectedItem.ItemSerializerRepresentation);

                this.AffectedItems = new[] { new AffectedItem { Id = serializer.SerializeToString(affectedItem) } };

                this.UserDefinedMetadata = metadata.Select(_ => new MetadataItem(_.Key, _.Value)).ToArray();

                log.Write(() => "Completed downloading the file");
            }
        }

        /// <inheritdoc />
        public string FilePath { get; set; }

        /// <inheritdoc />
        public AffectedItem[] AffectedItems { get; set; }

        /// <inheritdoc />
        public MetadataItem[] UserDefinedMetadata { get; set; }
    }
}
