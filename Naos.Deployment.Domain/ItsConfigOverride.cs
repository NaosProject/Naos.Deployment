// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItsConfigOverride.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
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
