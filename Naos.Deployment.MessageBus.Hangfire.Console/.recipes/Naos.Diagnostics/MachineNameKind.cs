// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MachineNameKind.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Diagnostics.Recipes
{
    /// <summary>
    /// Specifies the kind of machine name.
    /// </summary>
#if NaosDiagnosticsRecipes
    public
#else
    [System.CodeDom.Compiler.GeneratedCode("Naos.Diagnostics", "See package version number")]
    internal
#endif
    enum MachineNameKind
    {
        /// <summary>
        /// The NetBIOS name.
        /// </summary>
        NetBiosName,

        /// <summary>
        /// The fully qualified domain name.
        /// </summary>
        FullyQualifiedDomainName,

        /// <summary>
        /// The resolved localhost name.
        /// </summary>
        ResolvedLocalhostName,
    }
}