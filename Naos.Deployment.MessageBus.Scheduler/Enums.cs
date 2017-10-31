// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Enums.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Scheduler
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
        ProviderTag,
    }
}
