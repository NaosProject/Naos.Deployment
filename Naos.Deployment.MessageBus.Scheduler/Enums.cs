// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Enums.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arcology", Justification = "Spelling/name is correct.")]
        Arcology,

        /// <summary>
        /// Lookup instance IDs using the provider for the instance.
        /// </summary>
        Provider,
    }
}
