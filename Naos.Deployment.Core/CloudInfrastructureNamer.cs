// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloudInfrastructureNamer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;

    /// <summary>
    /// Composes easy to track names for cloud resources.
    /// </summary>
    public class CloudInfrastructureNamer
    {
        private readonly string baseName;

        private readonly string containerLocation;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudInfrastructureNamer"/> class.
        /// </summary>
        /// <param name="baseName">Base name to use.</param>
        /// <param name="containerLocation">Container location of the property.</param>
        public CloudInfrastructureNamer(string baseName, string containerLocation)
        {
            this.baseName = baseName;
            this.containerLocation = containerLocation;
        }

        /// <summary>
        /// Gets a name for the instance that is by convention easy to track.
        /// </summary>
        /// <returns>Name to apply to instance.</returns>
        public string GetInstanceName()
        {
            var name = string.Format("instance-{0}@{1}", this.baseName, this.containerLocation);
            return name;
        }

        /// <summary>
        /// Gets a name for the volume that is by convention easy to track.
        /// </summary>
        /// <param name="driveLetter">Drive letter of the volume to get name for.</param>
        /// <returns>Name to apply to volume.</returns>
        public string GetVolumeName(string driveLetter)
        {
            var name = string.Format("ebs-{2}-{0}@{1}", this.baseName, this.containerLocation, driveLetter);
            return name;
        }
    }
}