// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Serializer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

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
            return DefaultJsonSerializer.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Gets the object from the JSON with specific settings needed for project objects.
        /// </summary>
        /// <param name="type">Type of object to return.</param>
        /// <param name="json">JSON to deserialize.</param>
        /// <returns>Object of type T to be returned.</returns>
        public static object Deserialize(Type type, string json)
        {
            return DefaultJsonSerializer.DeserializeObject(json, type);
        }

        /// <summary>
        /// Serializes the provided object and returns JSON (using specific settings for this project).
        /// </summary>
        /// <typeparam name="T">Type of object to serialize.</typeparam>
        /// <param name="objectToSerialize">Object to serialize to JSON.</param>
        /// <returns>String of JSON.</returns>
        public static string Serialize<T>(T objectToSerialize)
        {
            return DefaultJsonSerializer.SerializeObject(objectToSerialize);
        }
    }
}
