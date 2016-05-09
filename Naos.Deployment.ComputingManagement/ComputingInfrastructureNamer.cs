// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComputingInfrastructureNamer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.ComputingManagement
{
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Composes easy to track names for computing resources.
    /// </summary>
    public class ComputingInfrastructureNamer
    {
        private readonly string baseName;

        private readonly string environment;

        private readonly string containerLocation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputingInfrastructureNamer"/> class.
        /// </summary>
        /// <param name="baseName">Base name to use.</param>
        /// <param name="environment">Environment being used.</param>
        /// <param name="containerLocation">Container location of the property.</param>
        public ComputingInfrastructureNamer(string baseName, string environment, string containerLocation)
        {
            ThrowOnInvalidName(baseName);
            this.baseName = baseName;
            this.environment = environment;
            this.containerLocation = containerLocation;
        }

        /// <summary>
        /// Gets a name for the instance that is by convention easy to track.
        /// </summary>
        /// <returns>Name to apply to instance.</returns>
        public string GetInstanceName()
        {
            var name = $"instance-{this.environment}-{this.baseName}@{this.containerLocation}";
            return name;
        }

        /// <summary>
        /// Gets a name for the volume that is by convention easy to track.
        /// </summary>
        /// <param name="driveLetter">Drive letter of the volume to get name for.</param>
        /// <returns>Name to apply to volume.</returns>
        public string GetVolumeName(string driveLetter)
        {
            var name = $"ebs-{this.environment}-{this.baseName}-{driveLetter}@{this.containerLocation}";
            return name;
        }

        private static void ThrowOnInvalidName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Cannot have a 'null' or empty name.");
            }

            if (name.StartsWith("-") || name.EndsWith("-"))
            {
                throw new ArgumentException("Cannot start or end the name in a dash (-) because it's an invalid URL subdomain.");
            }

            var manualInvalidCharsToTest = new[]
                                         {
                                             '.',
                                             '!',
                                             '@',
                                             '#',
                                             '$',
                                             '%',
                                             '^',
                                             '&',
                                             '*',
                                             '(',
                                             ')',
                                             '+',
                                             '=',
                                         };

            var invalidCharsToTest = Path.GetInvalidPathChars().ToList();
            invalidCharsToTest.AddRange(Path.GetInvalidFileNameChars());
            invalidCharsToTest.AddRange(manualInvalidCharsToTest);

            var invalidCharDetections = invalidCharsToTest.Where(name.Contains).ToList();
            if (invalidCharDetections.Any())
            {
                throw new ArgumentException(
                    "The name: " + name + " cannot contain the character(s): " + string.Join(",", invalidCharDetections));
            }
        }
    }
}