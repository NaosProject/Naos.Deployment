// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescriptionWithOverrides.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;

    using Naos.Packaging.Domain;

    /// <summary>
    /// Package description with initialization strategies added to override when calling deploy.
    /// </summary>
    public class PackageDescriptionWithOverrides : PackageDescription, IHaveInitializationStrategies, IHaveItsConfigOverrides
    {
        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public IReadOnlyCollection<InitializationStrategyBase> InitializationStrategies { get; set; }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public IReadOnlyCollection<ItsConfigOverride> ItsConfigOverrides { get; set; }
    }
}
