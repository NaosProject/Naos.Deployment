// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHaveInitializationStrategiesExtensionMethods.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Additional behavior to add on IHaveInitializationStrategies.
    /// </summary>
    public static class IHaveInitializationStrategiesExtensionMethods
    {
        /// <summary>
        /// Retrieves the initialization strategies matching the specified type.
        /// </summary>
        /// <typeparam name="T">Type of initialization strategy to look for.</typeparam>
        /// <param name="objectWithInitializationStrategies">Object to operate on.</param>
        /// <returns>Collection of initialization strategies matching the type specified.</returns>
        public static ICollection<T> GetInitializationStrategiesOf<T>(
            this IHaveInitializationStrategies objectWithInitializationStrategies) where T : InitializationStrategyBase
        {
            var ret =
                (objectWithInitializationStrategies.InitializationStrategies ?? new List<InitializationStrategyBase>())
                    .Select(strat => strat as T).Where(_ => _ != null).ToList();

            return ret;
        }
    }
}
