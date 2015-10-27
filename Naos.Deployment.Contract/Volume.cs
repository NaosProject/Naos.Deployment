// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Volume.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Object to describe a volume to attach to the instance.
    /// </summary>
    public class Volume
    {
        /// <summary>
        /// Gets or sets the drive letter of the volume.
        /// </summary>
        public string DriveLetter { get; set; }

        /// <summary>
        /// Gets or sets the size of the volume in gigabytes.
        /// </summary>
        public int SizeInGb { get; set; }

        /// <summary>
        /// Gets or sets the type of volume.
        /// </summary>
        public VolumeType Type { get; set; }
    }
}