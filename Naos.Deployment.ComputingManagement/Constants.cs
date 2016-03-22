// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.ComputingManagement
{
    /// <summary>
    /// Constants used for interacting with cloud.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Name of the tag for an environment.
        /// </summary>
        public const string EnvironmentTagKey = "Environment";

        /// <summary>
        /// Name of the tag for an instance name in AWS.
        /// </summary>
        internal const string NameTagKey = Naos.AWS.Core.Constants.NameTagKey;
    }
}