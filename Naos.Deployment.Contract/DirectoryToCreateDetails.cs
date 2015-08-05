// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DirectoryToCreateDetails.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Container to describe a directory to create.
    /// </summary>
    public class DirectoryToCreateDetails
    {
        /// <summary>
        /// Gets or sets the full path of the directory.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets the user account name that will have "Full Control" of the directory.
        /// </summary>
        public string FullControlAccount { get; set; }
    }
}