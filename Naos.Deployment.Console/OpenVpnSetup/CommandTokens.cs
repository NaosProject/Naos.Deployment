﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandTokens.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// <auto-generated>
//   Sourced from NuGet package. Will be overwritten with package update except in Naos.Deployment.Recipes.OpenVpnSetup source.
// </auto-generated>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    /// <summary>
    /// Contains various tokens used in commands.
    /// </summary>
#if !NaosDeploymentConsole
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [System.CodeDom.Compiler.GeneratedCode("Naos.Deployment.Recipes.OpenVpnSetup", "See package version number")]
#endif
    public static class CommandTokens
    {
        /// <summary>
        /// The token that represents a username in various commands.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Username")]
        public static readonly string Username = "[USER_NAME]";

        /// <summary>
        /// The token that represents a password in various commands.
        /// </summary>
        public static readonly string Password = "[PASSWORD]";

        /// <summary>
        /// The token that represents a hostname (e.g. vpn.example.com) in various commands.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Hostname")]
        public static readonly string Hostname = "[HOSTNAME]";

        /// <summary>
        /// The token that represents a subnet (e.g. "10.31.0.0/16") in various commands.
        /// </summary>
        public static readonly string Subnet = "[SUBNET]";

        /// <summary>
        /// The token that represents the index in a 0-based array in various commands.
        /// </summary>
        public static readonly string ArrayIndex = "[ARRAY_INDEX]";

        /// <summary>
        /// The token that represents a PEM-encoded cryptographic resource (e.g. a private key, a certificate).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pem", Justification = "Spelling/name is correct.")]
        public static readonly string CryptographicResourcePemEncoded = "[PEM_ENCODED_CRYPTOGRAPHIC_RESOURCE]";

        /// <summary>
        /// The token that represents an OpenVPN Access Server license key.
        /// </summary>
        // ReSharper disable once StringLiteralTypo
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Vpn", Justification = "Spelling/name is correct.")]
        public static readonly string OpenVpnAccessServerLicenseKey = "[OVPN_LICENSE_KEY]";
    }
}
