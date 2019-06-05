// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingHelper.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using Naos.Deployment.Domain;
    using Naos.Serialization.Domain;
    using Naos.Serialization.Json;

    /// <summary>
    /// Helper methods for using a package manager.
    /// </summary>
    public static class LoggingHelper
    {
        private static readonly IStringSerialize Serializer = new NaosJsonSerializer(typeof(NaosDeploymentTrackingJsonConfiguration), UnregisteredTypeEncounteredStrategy.Attempt);

        /// <summary>
        /// Serializes the provided object to a string to be logged.
        /// </summary>
        /// <param name="objectToSerialize">Object to serialize.</param>
        /// <returns>Serialized object as string.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object", Justification = "Spelling/name is correct.")]
        public static string SerializeToString(object objectToSerialize)
        {
            return Serializer.SerializeToString(objectToSerialize);
        }
    }
}
