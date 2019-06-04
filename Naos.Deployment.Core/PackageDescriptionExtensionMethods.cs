// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescriptionExtensionMethods.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    /// <summary>
    /// Additional behavior to add on package descriptions and derivatives.
    /// </summary>
    public static class PackageDescriptionExtensionMethods
    {
        /// <summary>
        /// Converts from PackageDescriptionWithStrategies to PackageDescription collection.
        /// </summary>
        /// <param name="withStrategies">Original collection to use.</param>
        /// <returns>New collection without strategies.</returns>
        public static IReadOnlyCollection<PackageDescription> WithoutStrategies(
            this IReadOnlyCollection<PackageDescriptionWithOverrides> withStrategies)
        {
            var ret = withStrategies.Select(_ => new PackageDescription { Id = _.Id, Version = _.Version }).ToList();
            return ret;
        }
    }
}
