// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHaveInitializationStrategies.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for a collection of initialization strategies.
    /// </summary>
    public interface IHaveInitializationStrategies
    {
        /// <summary>
        /// Gets or sets the initialization strategies.
        /// </summary>
        ICollection<InitializationStrategyBase> InitializationStrategies { get; set; }
    }
}