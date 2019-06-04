// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionsToConsoleAbstraction.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Hangfire.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using CLAP;

    using Its.Configuration;

    using Naos.AWS.S3;
    using Naos.FileJanitor.Domain;
    using Naos.FileJanitor.MessageBus.Scheduler;
    using Naos.FileJanitor.S3;
    using Naos.Logging.Domain;
    using Naos.Logging.Persistence;
    using Naos.Recipes.RunWithRetry;
    using Naos.Serialization.Factory;

    using static System.FormattableString;

    /// <summary>
    /// Abstraction for use with <see cref="CLAP" /> to provide basic command line interaction.
    /// </summary>
    public partial class ConsoleAbstraction
    {
        /// <summary>
        /// Archive a directory into a file.
        /// </summary>
        /// <param name="sourceDirectoryPath">Path to archive (must be a directory).</param>
        /// <param name="targetFilePath">File path to archive to (must NOT exist).</param>
        /// <param name="directoryArchiveKind">Kind of archive.</param>
        /// <param name="archiveCompressionKind">Kind of compression.</param>
        /// <param name="environment">Sets the Its.Configuration precedence to use specific settings.</param>
        /// <param name="debug">Launches the debugger.</param>
        [Verb(Aliases = "archive", Description = "Archive a directory into a file.")]
        public static void Archive(
            [Required] [Aliases("source")] [Description("Path to archive (must be a directory).")] string sourceDirectoryPath,
            [Required] [Aliases("target")] [Description("File path to archive to (must NOT exist).")] string targetFilePath,
            [DefaultValue(DirectoryArchiveKind.DotNetZipFile)] [Aliases("")] [Description("Kind of archive.")] DirectoryArchiveKind directoryArchiveKind,
            [DefaultValue(ArchiveCompressionKind.Fastest)] [Aliases("")] [Description("Kind of compression.")] ArchiveCompressionKind archiveCompressionKind,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug)
        {
            // Only do console logging since these are intended to be additions to the primary functionality of processing messages from a queue
            CommonSetup(
                debug,
                environment,
                new LogWritingSettings(
                    new[]
                        {
                            new ConsoleLogConfig(
                                new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                                new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                                {
                                    { LogItemKind.String, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                    { LogItemKind.Object, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                },
                                new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                                {
                                    { LogItemKind.Exception, null },
                                }),
                        }));

            var archiver = ArchiverFactory.Instance.BuildArchiver(directoryArchiveKind, archiveCompressionKind);
            var archivedDirectory = Run.TaskUntilCompletion(archiver.ArchiveDirectoryAsync(sourceDirectoryPath, targetFilePath, true, Encoding.UTF8));

            PrintArguments(archivedDirectory, Invariant($"Result of archiving of: {sourceDirectoryPath}"));
        }

        /// <summary>
        /// Restores a file into a directory.
        /// </summary>
        /// <param name="sourceFilePath">File path to restore from (must be archive file).</param>
        /// <param name="targetDirectoryPath">Path to restore to (must be a directory AND not exist).</param>
        /// <param name="directoryArchiveKind">Kind of archive.</param>
        /// <param name="archiveCompressionKind">Kind of compression.</param>
        /// <param name="environment">Sets the Its.Configuration precedence to use specific settings.</param>
        /// <param name="debug">Launches the debugger.</param>
        [Verb(Aliases = "restore", Description = "Restores a file into a directory.")]
        public static void Restore(
            [Required] [Aliases("source")] [Description("File path to restore from (must be archive file).")] string sourceFilePath,
            [Required] [Aliases("target")] [Description("Path to restore to (must be a directory AND not exist).")] string targetDirectoryPath,
            [DefaultValue(DirectoryArchiveKind.DotNetZipFile)] [Aliases("")] [Description("Kind of archive.")] DirectoryArchiveKind directoryArchiveKind,
            [DefaultValue(ArchiveCompressionKind.Fastest)] [Aliases("")] [Description("Kind of compression.")] ArchiveCompressionKind archiveCompressionKind,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug)
        {
            // Only do console logging since these are intended to be additions to the primary functionality of processing messages from a queue
            CommonSetup(
                debug,
                environment,
                new LogWritingSettings(
                    new[]
                    {
                        new ConsoleLogConfig(
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.String, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                { LogItemKind.Object, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                            },
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.Exception, null },
                            }),
                    }));

            var archiver = ArchiverFactory.Instance.BuildArchiver(directoryArchiveKind, archiveCompressionKind);
            var archivedDirectory = new ArchivedDirectory(directoryArchiveKind, archiveCompressionKind, sourceFilePath, true, Encoding.UTF8.WebName);
            Run.TaskUntilCompletion(archiver.RestoreDirectoryAsync(archivedDirectory, targetDirectoryPath));
        }

        /// <summary>
        /// Store a file in S3.
        /// </summary>
        /// <param name="filePath">File path to store (MUST be a file - cannot be used with directoryPath).</param>
        /// <param name="directoryPath">File path to store (MUST be a directory - cannot be used with filePath).</param>
        /// <param name="containerLocation">Location of container to use.</param>
        /// <param name="container">Container to use.</param>
        /// <param name="key">Optional key to store as; DEFAULT is file name.</param>
        /// <param name="userDefinedMetadataJson">User defined metadata (array of MetadataItem's in Config File JSON).</param>
        /// <param name="hashingAlgorithmNames">HashAlogirthmNames to use; MD5, SHA1, SHA256, etc.</param>
        /// <param name="directoryArchiveKind">Kind of archive if directoryPath used.</param>
        /// <param name="archiveCompressionKind">Kind of compression if directoryPath used.</param>
        /// <param name="environment">Sets the Its.Configuration precedence to use specific settings.</param>
        /// <param name="debug">Launches the debugger.</param>
        [Verb(Aliases = "store", Description = "Store a file in S3.")]
        public static void StoreFileOrDirectory(
            [Aliases("file")] [Description("File path to store (MUST be a file - cannot be used with directoryPath).")] string filePath,
            [Aliases("directory")] [Description("File path to store (MUST be a directory - cannot be used with filePath).")] string directoryPath,
            [Required] [Aliases("location")] [Description("Container location to store the file in.")] string containerLocation,
            [Required] [Aliases("")] [Description("Container to store the file in.")] string container,
            [Aliases("")] [Description("Key to store file as; default will be file OR directory name.")] [DefaultValue(null)] string key,
            [Aliases("metadata")] [Description("User defined metadata (array of MetadataItem's in Config File JSON).")] [DefaultValue(null)] string userDefinedMetadataJson,
            [Aliases("hash")] [Description("HashAlogirthmNames to use; MD5, SHA1, SHA256, etc.")] string[] hashingAlgorithmNames,
            [DefaultValue(DirectoryArchiveKind.DotNetZipFile)] [Aliases("")] [Description("Kind of archive if directoryPath used.")] DirectoryArchiveKind directoryArchiveKind,
            [DefaultValue(ArchiveCompressionKind.Fastest)] [Aliases("")] [Description("Kind of compression if directoryPath used.")] ArchiveCompressionKind archiveCompressionKind,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug)
        {
            // Only do console logging since these are intended to be additions to the primary functionality of processing messages from a queue
            CommonSetup(
                debug,
                environment,
                new LogWritingSettings(
                    new[]
                    {
                        new ConsoleLogConfig(
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.String, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                { LogItemKind.Object, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                            },
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.Exception, null },
                            }),
                    }));

            var settings = Settings.Get<FileJanitorMessageHandlerSettings>();

            var fileManager = new FileManager(settings.UploadAccessKey, settings.UploadSecretKey);

            var serializer = SerializerFactory.Instance.BuildSerializer(Config.ConfigFileSerializationDescription);
            var userDefinedMetadata = string.IsNullOrWhiteSpace(userDefinedMetadataJson) ? null : serializer.Deserialize<IReadOnlyCollection<MetadataItem>>(userDefinedMetadataJson);

            if (!string.IsNullOrWhiteSpace(filePath) && string.IsNullOrWhiteSpace(directoryPath))
            {
                Run.TaskUntilCompletion(FileExchanger.StoreFile(fileManager, filePath, containerLocation, container, key, userDefinedMetadata, hashingAlgorithmNames));
            }
            else if (!string.IsNullOrWhiteSpace(directoryPath) && string.IsNullOrWhiteSpace(filePath))
            {
                Run.TaskUntilCompletion(FileExchanger.StoreDirectory(fileManager, directoryPath, directoryArchiveKind, archiveCompressionKind, true, Encoding.UTF8, containerLocation, container, key, userDefinedMetadata, hashingAlgorithmNames));
            }
            else
            {
                throw new ArgumentException(Invariant($"Must specify either a {nameof(filePath)} or {nameof(directoryPath)} to store."));
            }
        }

        /// <summary>
        /// Removes old files.
        /// </summary>
        /// <param name="targetPath">File path to fetch to (must be valid directory path if usin restore switch).</param>
        /// <param name="containerLocation">Location of container to use.</param>
        /// <param name="container">Container to use.</param>
        /// <param name="key">Key of file to fetch (cannot be used with prefix).</param>
        /// <param name="prefix">Search prefix to use to file file (cannot be used with key).</param>
        /// <param name="multipleKeysFoundStrategy">Strategy on choosing file when used with prefix.</param>
        /// <param name="restoreArchive">Restore the archive to the target path as a directory (MUST be an archive file).</param>
        /// <param name="environment">Sets the Its.Configuration precedence to use specific settings.</param>
        /// <param name="debug">Launches the debugger.</param>
        [Verb(Aliases = "fetch", Description = "Archive a directory into a file.")]
        public static void FetchFileOrArchive(
            [Required][Aliases("path")] [Description("File path to fetch to (must be valid directory path if using restore switch).")] string targetPath,
            [Required] [Aliases("location")] [Description("Container location to store the file in.")] string containerLocation,
            [Required] [Aliases("")] [Description("Container to store the file in.")] string container,
            [Aliases("")] [Description("Key of file to fetch (cannot be used with prefix).")] string key,
            [Aliases("")] [Description("Search prefix to use to file file (cannot be used with key).")] string prefix,
            [Aliases("")] [Description("Strategy on choosing file when used with prefix.")] [DefaultValue(MultipleKeysFoundStrategy.FirstSortedDescending)] MultipleKeysFoundStrategy multipleKeysFoundStrategy,
            [Aliases("restore")] [Description("Restore the archive to the target path as a directory (MUST be an archive file).")] [DefaultValue(false)] bool restoreArchive,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug)
        {
            // Only do console logging since these are intended to be additions to the primary functionality of processing messages from a queue
            CommonSetup(
                debug,
                environment,
                new LogWritingSettings(
                    new[]
                    {
                        new ConsoleLogConfig(
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.String, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                { LogItemKind.Object, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                            },
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.Exception, null },
                            }),
                    }));

            var settings = Settings.Get<FileJanitorMessageHandlerSettings>();

            var fileManager = new FileManager(settings.DownloadAccessKey, settings.DownloadSecretKey);

            if (!string.IsNullOrWhiteSpace(prefix) && string.IsNullOrWhiteSpace(key))
            {
                var foundFile = Run.TaskUntilCompletion(FileExchanger.FindFile(fileManager, containerLocation, container, prefix, multipleKeysFoundStrategy));
                key = foundFile.Key;
                Its.Log.Instrumentation.Log.Write(() => Invariant($"Chose prefix ({prefix}) match: {key}"));
            }
            else if (!string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(prefix))
            {
                /* no-op */
            }
            else
            {
                throw new ArgumentException(Invariant($"Must specify either a {nameof(prefix)} or {nameof(key)} to fetch."));
            }

            var downloadTarget = restoreArchive
                                     ? Path.Combine(
                                         Path.GetDirectoryName(targetPath) ?? Path.GetTempPath(),
                                         "FileJanitor-FetchFileAndRestore-" + Guid.NewGuid() + ".tmp")
                                     : targetPath;

            Run.TaskUntilCompletion(FileExchanger.FetchFile(fileManager, containerLocation, container, key, downloadTarget));

            if (restoreArchive)
            {
                var metadata = Run.TaskUntilCompletion(FileExchanger.FetchMetadata(fileManager, containerLocation, container, key));
                Run.TaskUntilCompletion(FileExchanger.RestoreDownload(downloadTarget, targetPath, metadata));
            }
        }

        /// <summary>
        /// Removes old files.
        /// </summary>
        /// <param name="rootPath">The root path to evaluate (must be a directory).</param>
        /// <param name="retentionWindow">The time to retain files (in format dd:hh:mm).</param>
        /// <param name="recursive">Whether or not to evaluate files recursively on the path.</param>
        /// <param name="deleteEmptyDirectories">Whether or not to delete directories that are or become empty during cleanup.</param>
        /// <param name="dateRetrievalStrategy">The date retrieval strategy to use on files.</param>
        /// <param name="environment">Sets the Its.Configuration precedence to use specific settings.</param>
        /// <param name="debug">Launches the debugger.</param>
        [Verb(Aliases = "clean", Description = "Removes old files.")]
        public static void Cleanup(
            [Required] [Aliases("")] [Description("The root path to evaluate (must be a directory).")] string rootPath,
            [Required] [Aliases("")] [Description("The time to retain files (in format dd:hh:mm).")] string retentionWindow,
            [DefaultValue(true)] [Aliases("")] [Description("Whether or not to evaluate files recursively on the path.")] bool recursive,
            [DefaultValue(false)] [Aliases("")] [Description("Whether or not to delete directories that are or become empty during cleanup.")] bool deleteEmptyDirectories,
            [DefaultValue(DateRetrievalStrategy.LastUpdateDate)] [Aliases("")] [Description("The date retrieval strategy to use on files.")] DateRetrievalStrategy dateRetrievalStrategy,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug)
        {
            // Only do console logging since these are intended to be additions to the primary functionality of processing messages from a queue
            CommonSetup(
                debug,
                environment,
                new LogWritingSettings(
                    new[]
                    {
                        new ConsoleLogConfig(
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.String, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                                { LogItemKind.Object, new[] { LogItemOrigin.ItsLogEntryPosted.ToString() } },
                            },
                            new Dictionary<LogItemKind, IReadOnlyCollection<string>>
                            {
                                { LogItemKind.Exception, null },
                            }),
                    }));

            var retentionWindowTimeSpan = ParseTimeSpanFromDayHourMinuteColonDelimited(retentionWindow);

            PrintArguments(
                new
                {
                    rootPath,
                    retentionWindowAsDayHourMinute = retentionWindow,
                    retentionWindowInDays = retentionWindowTimeSpan.TotalDays,
                    deleteEmptyDirectories,
                    recursive,
                    dateRetrievalStrategy,
                });

            FilePathJanitor.Cleanup(
                rootPath,
                retentionWindowTimeSpan,
                recursive,
                deleteEmptyDirectories,
                dateRetrievalStrategy);
        }
    }
}