// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Serializer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    using Newtonsoft.Json;

    using Spritely.Recipes;

    /// <summary>
    /// Specific serialization settings encapsulated for needs when using objects from this project..
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Gets the object from the JSON with specific settings needed for project objects.
        /// </summary>
        /// <typeparam name="T">Type of object to return.</typeparam>
        /// <param name="json">JSON to deserialize.</param>
        /// <returns>Object of type T to be returned.</returns>
        public static T Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            SetupDefaultSettings(false);
            
            var ret = JsonConvert.DeserializeObject<T>(json);

            return ret;
        }

        /// <summary>
        /// Gets the object from the JSON with specific settings needed for project objects.
        /// </summary>
        /// <param name="type">Type of object to return.</param>
        /// <param name="json">JSON to deserialize.</param>
        /// <returns>Object of type T to be returned.</returns>
        public static object Deserialize(Type type, string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            SetupDefaultSettings(false);

            var ret = JsonConvert.DeserializeObject(json, type);

            return ret;
        }

        /// <summary>
        /// Serializes the provided object and returns JSON (using specific settings for this project).
        /// </summary>
        /// <typeparam name="T">Type of object to serialize.</typeparam>
        /// <param name="objectToSerialize">Object to serialize to JSON.</param>
        /// <param name="compact">Optionally will create compact JSON (default is false; specifying false will use a more minimal representation).</param>
        /// <returns>String of JSON.</returns>
        public static string Serialize<T>(T objectToSerialize, bool compact = false)
        {
            SetupDefaultSettings(compact);

            var ret = JsonConvert.SerializeObject(objectToSerialize);

            return ret;
        }

        private static void SetupDefaultSettings(bool compact)
        {
            JsonConvert.DefaultSettings = () => compact ? JsonConfiguration.CompactSerializerSettings : JsonConfiguration.DefaultSerializerSettings;
        }
    }
}
