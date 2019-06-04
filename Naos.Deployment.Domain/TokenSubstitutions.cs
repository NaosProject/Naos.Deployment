// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TokenSubstitutions.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Globalization;

    /// <summary>
    /// Class to consolidate the tokens that are available to be substituted.
    /// </summary>
    public static class TokenSubstitutions
    {
        /// <summary>
        /// The environment being deployed into.
        /// </summary>
        public const string EnvironmentToken = "{environment}";

        /// <summary>
        /// The name of the created instance.
        /// </summary>
        public const string InstanceNameToken = "{instanceName}";

        /// <summary>
        /// The name of the created instance.
        /// </summary>
        public const string InstanceNumberToken = "{instanceNumber}";

        /// <summary>
        /// The account that the process of the Message Bus Handler Harness will run as.
        /// </summary>
        public const string HarnessAccountToken = "{harnessAccount}";

        /// <summary>
        /// The account that the process of an IIS AppPool will run as.
        /// </summary>
        public const string IisAccountToken = "{iisAccount}";

        /// <summary>
        /// Apply account substitutions to the provided string.
        /// </summary>
        /// <param name="stringToApplyTokenSubstitutions">Tokenized string to apply token substitutions to.</param>
        /// <param name="harnessAccount">Harness account to use for harness token.</param>
        /// <param name="iisAccount">IIS account to use for harness token.</param>
        /// <returns>Provided string with any found substitutions.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string", Justification = "Spelling/name is correct.")]
        public static string GetSubstitutedStringForAccounts(string stringToApplyTokenSubstitutions, string harnessAccount, string iisAccount)
        {
            var ret = stringToApplyTokenSubstitutions?.Replace(HarnessAccountToken, harnessAccount).Replace(IisAccountToken, iisAccount);

            return ret;
        }

        /// <summary>
        /// Apply DNS substitutions to the provided string.
        /// </summary>
        /// <param name="stringToApplyTokenSubstitutions">Tokenized string to apply token substitutions to.</param>
        /// <param name="environment">Environment being deployed to.</param>
        /// <param name="instanceName">Name of the created instance.</param>
        /// <param name="instanceNumber">The number of the instance (used when multiple instances are being created).</param>
        /// <returns>Provided string with any found substitutions.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string", Justification = "Spelling/name is correct.")]
        public static string GetSubstitutedStringForDns(string stringToApplyTokenSubstitutions, string environment, string instanceName, int instanceNumber)
        {
            var ret = stringToApplyTokenSubstitutions?.Replace("{instanceName}", instanceName)
                .Replace("{environment}", environment)
                .Replace("{instanceNumber}", instanceNumber.ToString(CultureInfo.CurrentCulture));

            return ret;
        }

        /// <summary>
        /// Apply Channel Name substitutions to the provided string.
        /// </summary>
        /// <param name="stringToApplyTokenSubstitutions">Tokenized string to apply token substitutions to.</param>
        /// <param name="environment">Environment being deployed to.</param>
        /// <param name="instanceName">Name of the created instance.</param>
        /// <param name="instanceNumber">The number of the instance (used when multiple instances are being created).</param>
        /// <returns>Provided string with any found substitutions.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string", Justification = "Spelling/name is correct.")]
        public static string GetSubstitutedStringForChannelName(string stringToApplyTokenSubstitutions, string environment, string instanceName, int instanceNumber)
        {
            var ret = stringToApplyTokenSubstitutions?.Replace("{instanceName}", instanceName)
                .Replace("{environment}", environment)
                .Replace("{instanceNumber}", instanceNumber.ToString(CultureInfo.CurrentCulture));

            return ret;
        }

        /// <summary>
        /// Apply path substitutions to the provided string.
        /// </summary>
        /// <param name="stringToApplyTokenSubstitutions">Tokenized string to apply token substitutions to.</param>
        /// <param name="deploymentDriveLetter">Volume drive letter that is being used for deploying package.</param>
        /// <returns>Provided string with any found substitutions.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string", Justification = "Spelling/name is correct.")]
        public static string GetSubstitutedStringForPath(string stringToApplyTokenSubstitutions, string deploymentDriveLetter)
        {
            var ret = stringToApplyTokenSubstitutions?.Replace("{deploymentDriveLetter}", deploymentDriveLetter);

            return ret;
        }
    }
}