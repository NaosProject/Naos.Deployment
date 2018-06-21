// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MachineMemoryKind.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Diagnostics.Recipes
{
    /// <summary>
    /// Specifies the kind of machine memory.
    /// </summary>
#if NaosDiagnosticsRecipes
    public
#else
    [System.CodeDom.Compiler.GeneratedCode("Naos.Diagnostics", "See package version number")]
    internal
#endif
    enum MachineMemoryKind
    {
        /// <summary>
        /// The total physical memory.
        /// </summary>
        TotalPhysical,

        /// <summary>
        /// The total virtual memory.
        /// </summary>
        TotalVirtual,

        /// <summary>
        /// The available physical memory.
        /// </summary>
        AvailablePhysical,

        /// <summary>
        /// The avilable virtual memory.
        /// </summary>
        AvailableVirtual,
    }
}