// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingHelper.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using Naos.Serialization.Domain;
    using Naos.Serialization.Json;

    /// <summary>
    /// Helper methods for using a package manager.
    /// </summary>
    public static class LoggingHelper
    {
        private static readonly IStringSerialize Serializer = new NaosJsonSerializer();

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
