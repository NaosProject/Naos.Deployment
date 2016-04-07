// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Enums.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    /// <summary>
    /// Enumeration of the pathway to lookup an instance by name.
    /// </summary>
    public enum InstanceNameLookupSource
    {
        /// <summary>
        /// Lookup instance IDs using the name in the arcology.
        /// </summary>
        Arcology,

        /// <summary>
        /// Lookup instance IDs using the name in the tag on the provider for the instance.
        /// </summary>
        ProviderTag
    }
}
