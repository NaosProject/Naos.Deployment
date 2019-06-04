// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyDirectoryToCreate.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Custom extension of the InitializationStrategyBase to accommodate adding a directory with full control given to a specific account.
    /// </summary>
    public class InitializationStrategyDirectoryToCreate : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets directories to create on file system.
        /// </summary>
        public DirectoryToCreateDetails DirectoryToCreate { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategyDirectoryToCreate
                          {
                              DirectoryToCreate = (DirectoryToCreateDetails)this.DirectoryToCreate.Clone(),
                          };
            return ret;
        }
    }
}