// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescriptionIdOnlyEqualityComparer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;

    using Naos.Deployment.Contract;

    internal class PackageDescriptionIdOnlyEqualityComparer : IEqualityComparer<PackageDescription>
    {
        public bool Equals(PackageDescription x, PackageDescription y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Id == y.Id;
        }

        public int GetHashCode(PackageDescription obj)
        {
            var id = obj == null ? null : obj.Id;
            var version = obj == null ? null : obj.Version;
            var hashCode = new Tuple<string, string>(id, version).GetHashCode();
            return hashCode;
        }
    }
}