// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceType.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Model object to describe the type/caliber of machine to provision.
    /// </summary>
    public class InstanceType
    {
        /// <summary>
        /// Gets or sets the minimum number of virtual cores necessary.
        /// </summary>
        public int? VirtualCores { get; set; }

        /// <summary>
        /// Gets or sets the minimum amount of RAM in gigabytes.
        /// </summary>
        public int? RamInGb { get; set; }
    }
}