// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescriptionExtensionMethods.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
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
        public static ICollection<PackageDescription> WithoutStrategies(
            this ICollection<PackageDescriptionWithOverrides> withStrategies)
        {
            var ret = withStrategies.Select(_ => new PackageDescription { Id = _.Id, Version = _.Version }).ToList();
            return ret;
        }
    }
}
