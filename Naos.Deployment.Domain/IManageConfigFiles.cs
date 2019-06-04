// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManageConfigFiles.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Its.Configuration;

    using Naos.Serialization.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Interface needed for dealing with config files.
    /// </summary>
    public interface IManageConfigFiles
    {
        /// <summary>
        /// Deserialize the text of a config file to the specified object.
        /// </summary>
        /// <typeparam name="T">Type to deserialize into.</typeparam>
        /// <param name="configFileText">Text of the config file to use as input.</param>
        /// <returns>Object of specified type as it was deserialized from the text.</returns>
        T DeserializeConfigFileText<T>(string configFileText);

        /// <summary>
        /// Serializes a configuration object to text to be written to a config file.
        /// </summary>
        /// <param name="configToSerialize">Object to seriailze.</param>
        /// <returns>Text to be written to a configuration file.</returns>
        string SerializeConfigToFileText(object configToSerialize);

        /// <summary>
        /// Builds a config path with varying degrees of precision.
        /// </summary>
        /// <param name="rootPath">Optional root path to use.</param>
        /// <param name="precedence">Optional precedence to use, will choose lowest common precedence if none specified.</param>
        /// <param name="fileNameWithExtension">Optional filename to use.</param>
        /// <returns>Config path.</returns>
        string BuildConfigPath(string rootPath = null, string precedence = null, string fileNameWithExtension = null);

        /// <summary>
        /// Builds a precedence chain with optional front level environment precedence.
        /// </summary>
        /// <param name="environment">Optional root path to use.</param>
        /// <returns>Precedence chain.</returns>
        string[] BuildPrecedenceChain(string environment = null);

        /// <summary>
        /// Converts the string contents of a configuration file to the bytes that should be written on disk.
        /// </summary>
        /// <param name="fileContents">String contents of configuration file.</param>
        /// <returns>Bytes of configuration file to be written to disk.</returns>
        byte[] ConvertConfigFileTextToFileBytes(string fileContents);
    }

    /// <summary>
    /// Simple serializer provided implementation of <see cref="IManageConfigFiles" />.
    /// </summary>
    public class ConfigFileManager : IManageConfigFiles
    {
        private readonly string[] itsConfigPrecedenceAfterEnvironment;

        private readonly string configDirectoryName;

        private readonly ISerializeAndDeserialize serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigFileManager"/> class.
        /// </summary>
        /// <param name="itsConfigPrecedenceAfterEnvironment">Ordered list of values to provide to <see cref="Its.Configuration" /> <see cref="Settings.Precedence" /> after environment which will be first value during any precedence configuration.</param>
        /// <param name="configDirectoryName">Name of directory with config files.</param>
        /// <param name="serializer">Serializer to read configuration files in and out of objects.</param>
        public ConfigFileManager(string[] itsConfigPrecedenceAfterEnvironment, string configDirectoryName, ISerializeAndDeserialize serializer)
        {
            this.itsConfigPrecedenceAfterEnvironment = itsConfigPrecedenceAfterEnvironment ?? new string[0];
            this.configDirectoryName = configDirectoryName;
            this.serializer = serializer;
        }

        /// <inheritdoc cref="IManageConfigFiles" />
        public T DeserializeConfigFileText<T>(string configFileText)
        {
            return this.serializer.Deserialize<T>(configFileText);
        }

        /// <inheritdoc cref="IManageConfigFiles" />
        public string SerializeConfigToFileText(object configToSerialize)
        {
            return this.serializer.SerializeToString(configToSerialize);
        }

        /// <inheritdoc cref="IManageConfigFiles" />
        public string BuildConfigPath(string rootPath = null, string precedence = null, string fileNameWithExtension = null)
        {
            var localPrecedence = precedence ?? this.itsConfigPrecedenceAfterEnvironment.Last() ?? throw new ArgumentException(Invariant($"Must specify at least one item in {nameof(this.itsConfigPrecedenceAfterEnvironment)} if NOT specifying {nameof(precedence)}."));
            var ret = Path.Combine(rootPath ?? string.Empty, this.configDirectoryName, localPrecedence, fileNameWithExtension ?? string.Empty);
            return ret;
        }

        /// <inheritdoc cref="IManageConfigFiles" />
        public string[] BuildPrecedenceChain(string environment = null)
        {
            var front = string.IsNullOrWhiteSpace(environment) ? new string[0] : new[] { environment };
            return front.Concat(this.itsConfigPrecedenceAfterEnvironment).ToArray();
        }

        /// <inheritdoc cref="IManageConfigFiles" />
        public byte[] ConvertConfigFileTextToFileBytes(string fileContents)
        {
            var ret = Encoding.UTF8.GetBytes(fileContents);
            return ret;
        }
    }
}