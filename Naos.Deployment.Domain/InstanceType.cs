// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceType.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
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
        public double? RamInGb { get; set; }

        /// <summary>
        /// Gets or sets a specific image to use (must be used in conjunction with WindowsSku.SpecificImageSupplied)
        /// </summary>
        public string SpecificImageSystemId { get; set; }

        /// <summary>
        /// Gets or sets a specific instance type to use (will override VirtualCores and RamInGb settings).
        /// </summary>
        public string SpecificInstanceTypeSystemId { get; set; }

        /// <summary>
        /// Gets or sets the Windows SKU to use.
        /// </summary>
        public WindowsSku WindowsSku { get; set; }
    }
}