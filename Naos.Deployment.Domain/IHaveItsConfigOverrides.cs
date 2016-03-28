// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHaveItsConfigOverrides.cs" company="Naos">
//   Copyright 2015 Naos
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
        ICollection<ItsConfigOverride> ItsConfigOverrides { get; set; } 
    }
}
