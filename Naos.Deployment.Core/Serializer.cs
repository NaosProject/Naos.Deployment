// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Serializer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

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

            var serializerSettings = GetJsonSerializerSettings();

            var ret = JsonConvert.DeserializeObject<T>(json, serializerSettings);
            return ret;
        }

        /// <summary>
        /// Serializes the provided object and returns JSON (using specific settings for this project).
        /// </summary>
        /// <typeparam name="T">Type of object to serialize.</typeparam>
        /// <param name="objectToSerialize">Object to serialize to JSON.</param>
        /// <param name="indented">Optionally indents the JSON (default is true; specifying false will put all on one line).</param>
        /// <returns>String of JSON.</returns>
        public static string Serialize<T>(T objectToSerialize, bool indented = true) where T : class
        {
            var serializerSettings = GetJsonSerializerSettings(indented);

            var ret = JsonConvert.SerializeObject(objectToSerialize, serializerSettings);
            return ret;
        }

        private static JsonSerializerSettings GetJsonSerializerSettings(bool indented = true)
        {
            var ret = new JsonSerializerSettings
                          {
                              ContractResolver = new CamelCasePropertyNamesContractResolver(),
                              Converters =
                                  new List<JsonConverter>
                                      {
                                          new StringEnumConverter
                                              {
                                                  CamelCaseText
                                                      =
                                                      true
                                              },
                                          new KnownTypeConverter(),
                                      }
                          };

            if (indented)
            {
                ret.Formatting = Formatting.Indented;
            }

            return ret;
        }
    }
}
