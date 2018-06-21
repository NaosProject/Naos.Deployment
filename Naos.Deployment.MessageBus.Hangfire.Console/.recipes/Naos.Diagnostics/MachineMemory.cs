// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MachineMemory.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Diagnostics.Recipes
{
    using System.Collections.Generic;

    using Microsoft.VisualBasic.Devices;

    /// <summary>
    /// Uses various methods to get the memory of a machine.
    /// </summary>
#if NaosDiagnosticsRecipes
    public
#else
    [System.CodeDom.Compiler.GeneratedCode("Naos.Diagnostics", "See package version number")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal
#endif
    static class MachineMemory
    {
        private const decimal DivideByToGetGb = 1024m * 1024m * 1024m;

        /// <summary>
        /// Gets the total physical memory of this machine in gigabytes.
        /// </summary>
        /// <returns>
        /// The total memory of this machine in gigabytes.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Gb", Justification = "Name/spelling is correct.")]
        public static IReadOnlyDictionary<MachineMemoryKind, decimal> GetMachineMemoryInGb()
        {
            // this is only in VisualBasic...
            var computerInfo = new ComputerInfo();
            var result = new Dictionary<MachineMemoryKind, decimal>
                             {
                                 { MachineMemoryKind.TotalPhysical, computerInfo.TotalPhysicalMemory / DivideByToGetGb },
                                 { MachineMemoryKind.AvailablePhysical, computerInfo.AvailablePhysicalMemory / DivideByToGetGb },
                                 { MachineMemoryKind.TotalVirtual, computerInfo.TotalVirtualMemory / DivideByToGetGb },
                                 { MachineMemoryKind.AvailableVirtual, computerInfo.AvailableVirtualMemory / DivideByToGetGb },
                             };

            return result;
        }
    }
}
