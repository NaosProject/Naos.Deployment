// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItsConfigOverride.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Model object to override an entire file in Its.Configuration.
    /// </summary>
    public class ItsConfigOverride
    {
        /// <summary>
        /// Gets or sets the file name to override.
        /// </summary>
        public string FileNameWithoutExtension { get; set; }

        /// <summary>
        /// Gets or sets the contents of JSON file to write.
        /// </summary>
        public string FileContentsJson { get; set; }
    }
}
