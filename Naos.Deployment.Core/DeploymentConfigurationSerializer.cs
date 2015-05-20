// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationSerializer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;

    using Naos.Deployment.Contract;

    using Newtonsoft.Json;

    /// <summary>
    /// Necessary logic to rehydrate deployment configurations.
    /// </summary>
    public static class DeploymentConfigurationSerializer
    {
        /// <summary>
        /// Gets the object from the JSON (can't use Json.Net directly due to InitializationStrategy being abstract.
        /// </summary>
        /// <param name="json">JSON to deserialize.</param>
        /// <returns>DeploymentConfiguration from provided JSON.</returns>
        public static DeploymentConfiguration DeserializeDeploymentConfiguration(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new KnownTypeConverter());
            var ret = JsonConvert.DeserializeObject<DeploymentConfiguration>(json, settings);
            return ret;
        }
    }
}
