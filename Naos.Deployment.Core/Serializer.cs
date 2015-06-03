// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Serializer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
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

            var serializerSettings = GetJsonSerializerSettings();

            var ret = JsonConvert.DeserializeObject(json, type, serializerSettings);
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

        /// <summary>
        /// Sets the default settings for JsonConvert to be the custom settings.
        /// </summary>
        public static void UpdateNewtonsoftJsonConvertDefaultsToCustomSettings()
        {
            JsonConvert.DefaultSettings = () => GetJsonSerializerSettings(true);
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

        /// <summary>
        /// TAKEN FROM: http://StackOverflow.com/a/17247339/1442829
        /// ---
        /// This requires the base type it's used on to declare all of the types it might use...
        /// ---
        /// Use KnownType Attribute to match a derived class based on the class given to the serializer
        /// Selected class will be the first class to match all properties in the json object.
        /// </summary>
        public class KnownTypeConverter : JsonConverter
        {
            /// <inheritdoc />
            public override bool CanConvert(Type objectType)
            {
                return System.Attribute.GetCustomAttributes(objectType).Any(v => v is KnownTypeAttribute);
            }

            /// <inheritdoc />
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                // Load JObject from stream
                var jsonObject = JObject.Load(reader);

                // Create target object based on JObject
                var attributes = Attribute.GetCustomAttributes(objectType);

                // Displaying output. 
                foreach (var attribute in attributes)
                {
                    if (attribute is KnownTypeAttribute)
                    {
                        var knownTypeAttribute = (KnownTypeAttribute)attribute;
                        var props = knownTypeAttribute.Type.GetProperties();
                        var found = true;
                        foreach (var f in jsonObject)
                        {
                            if (!props.Any(z => z.Name == f.Key))
                            {
                                found = false;
                                break;
                            }
                        }

                        if (found)
                        {
                            var target = Activator.CreateInstance(knownTypeAttribute.Type);
                            serializer.Populate(jsonObject.CreateReader(), target);
                            return target;
                        }
                    }
                }

                throw new ArgumentException("Invalid scenario encountered trying to deserialize: " + reader.ToString());
            }

            /// <inheritdoc />
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var jsonObject = JObject.FromObject(value);
                jsonObject.WriteTo(writer);
            }
        }
    }
}
