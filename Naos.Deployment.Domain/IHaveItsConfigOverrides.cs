// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHaveItsConfigOverrides.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for adding Its.Configuration overrides to a class.
    /// </summary>
    public interface IHaveItsConfigOverrides
    {
        /// <summary>
        /// Gets or sets a collection of Its.Configuration overrides.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        IReadOnlyCollection<ItsConfigOverride> ItsConfigOverrides { get; set; }
    }
}
