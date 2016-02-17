// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescriptionWithOverrides.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    using Naos.Packaging.Domain;

    /// <summary>
    /// Package description with initialization strategies added to override when calling deploy.
    /// </summary>
    public class PackageDescriptionWithOverrides : PackageDescription, IHaveInitializationStrategies, IHaveItsConfigOverrides
    {
        /// <inheritdoc />
        public ICollection<InitializationStrategyBase> InitializationStrategies { get; set; }

        /// <inheritdoc />
        public ICollection<ItsConfigOverride> ItsConfigOverrides { get; set; }
    }
}
