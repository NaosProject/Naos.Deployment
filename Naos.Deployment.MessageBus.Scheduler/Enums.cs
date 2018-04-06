// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Enums.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Scheduler
{
    /// <summary>
    /// Enumeration of the pathway to lookup an instance.
    /// </summary>
    public enum InstanceLookupSource
    {
        /// <summary>
        /// Lookup instance IDs using the arcology.
        /// </summary>
        Arcology,

        /// <summary>
        /// Lookup instance IDs using the provider for the instance.
        /// </summary>
        Provider,
    }
}
