// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DirectoryToCreateDetails.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    /// <summary>
    /// Container to describe a directory to create.
    /// </summary>
    public class DirectoryToCreateDetails : ICloneable
    {
        /// <summary>
        /// Gets or sets the full path of the directory.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets the user account name that will have "Full Control" of the directory.
        /// </summary>
        public string FullControlAccount { get; set; }

        /// <inheritdoc />
        public object Clone()
        {
            var ret = new DirectoryToCreateDetails
                          {
                              FullControlAccount = this.FullControlAccount,
                              FullPath = this.FullPath,
                          };
            return ret;
        }
    }
}
