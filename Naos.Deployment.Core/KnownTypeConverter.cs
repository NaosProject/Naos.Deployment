// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnownTypeConverter.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(objectType);  // Reflection. 

            // Displaying output. 
            foreach (System.Attribute attr in attrs)
            {
                if (attr is KnownTypeAttribute)
                {
                    KnownTypeAttribute k = (KnownTypeAttribute)attr;
                    var props = k.Type.GetProperties();
                    bool found = true;
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
                        var target = Activator.CreateInstance(k.Type);
                        serializer.Populate(jsonObject.CreateReader(), target);
                        return target;
                    }
                }
            }

            throw new ArgumentException("Invalid scenerio encountered trying to deserialize: " + reader.ToString());
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
