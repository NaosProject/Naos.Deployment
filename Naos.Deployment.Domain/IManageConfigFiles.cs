// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManageConfigFiles.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using Its.Configuration;

    using Naos.Serialization.Domain;

    /// <summary>
    /// Interface needed for dealing with config files.
    /// </summary>
    public interface IManageConfigFiles
    {
        /// <summary>
        /// Gets the ordered list of values to provide to <see cref="Its.Configuration" /> <see cref="Settings.Precedence" /> after environment which will be first value during any precedence configuration.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It is an array to match the Its.Configuration.Settings Property.")]
        string[] ItsConfigPrecedenceAfterEnvironment { get; }

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
    }

    /// <summary>
    /// Simple serializer provided implementation of <see cref="IManageConfigFiles" />.
    /// </summary>
    public class ConfigFileManager : IManageConfigFiles
    {
        private readonly ISerializeAndDeserialize serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigFileManager"/> class.
        /// </summary>
        /// <param name="itsConfigPrecedenceAfterEnvironment">Ordered list of values to provide to <see cref="Its.Configuration" /> <see cref="Settings.Precedence" /> after environment which will be first value during any precedence configuration.</param>
        /// <param name="serializer">Serializer to read configuration files in and out of objects.</param>
        public ConfigFileManager(string[] itsConfigPrecedenceAfterEnvironment, ISerializeAndDeserialize serializer)
        {
            this.ItsConfigPrecedenceAfterEnvironment = itsConfigPrecedenceAfterEnvironment;
            this.serializer = serializer;
        }

        /// <inheritdoc cref="IManageConfigFiles" />
        public string[] ItsConfigPrecedenceAfterEnvironment { get; private set; }

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
    }
}