// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyReplaceTokenInFiles.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Strategy to replace tokens in files, supports standard runtime tokens in replacement value.
    /// </summary>
    public class InitializationStrategyReplaceTokenInFiles : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the search pattern to use when finding files to replace tokens in.
        /// </summary>
        public string FileSearchPattern { get; set; }

        /// <summary>
        /// Gets or sets the token to replace.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the replacement value to use.
        /// </summary>
        public string Replacement { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategyReplaceTokenInFiles
            {
                              FileSearchPattern = this.FileSearchPattern,
                              Token = this.Token,
                              Replacement = this.Replacement,
                          };
            return ret;
        }
    }
}
