// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RecordingMachineManagerFactory.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Naos.MachineManagement.Domain;
    using Naos.MachineManagement.Local;

    using static System.FormattableString;
    using IManageMachines = Naos.MachineManagement.Domain.IManageMachines;

    /// <summary>
    /// Implementation of <see cref="ICreateMachineManagers" /> that just records the operations for manual use.
    /// </summary>
    public class RecordingMachineManagerFactory : ICreateMachineManagers
    {
        private readonly string outputPath;
        private int index;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingMachineManagerFactory" /> class.
        /// </summary>
        /// <param name="outputPath">Path to output files to.</param>
        public RecordingMachineManagerFactory(string outputPath)
        {
            this.outputPath = outputPath;
            this.index = 0;
        }

        /// <inheritdoc />
        public IManageMachines CreateMachineManager(MachineProtocol machineProtocol, string address, string userName, string password)
        {
            switch (machineProtocol)
            {
                case MachineProtocol.WinRm:
                    this.index = this.index + 1;
                    return new RecordPowershellMachineManager(this.index, this.outputPath);
                default:
                    throw new NotSupportedException(FormattableString.Invariant($"{nameof(MachineProtocol)}: {machineProtocol} is not supported."));
            }
        }
    }

    /// <summary>
    /// Implementation of <see cref="IManageMachines" /> that just records the operations for manual use.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Powershell", Justification = "Spelling/name is correct.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Is disposed correctly.")]
    public class RecordPowershellMachineManager : IManageMachines
    {
        private readonly int groupIndex;
        private readonly string outputPath;
        private readonly IManageMachines localMachineManager;
        private int index;

        /// <inheritdoc />
        public string Address => "localhost";

        /// <inheritdoc />
        public MachineProtocol MachineProtocol => MachineProtocol.Unknown;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordPowershellMachineManager" /> class.
        /// </summary>
        /// <param name="groupIndex">Group number of execution sets.</param>
        /// <param name="outputPath">Path to write the files to.</param>
        public RecordPowershellMachineManager(int groupIndex, string outputPath)
        {
            this.groupIndex = groupIndex;
            this.outputPath = outputPath;

            this.localMachineManager = new LocalMachineManager();
        }

        /// <inheritdoc />
        public void Reboot(bool force = true)
        {
            File.WriteAllText(this.GetPath("Reboot"), string.Empty);
        }

        /// <inheritdoc />
        public void SendFile(string filePathOnTargetMachine, byte[] fileContents, bool appended = false, bool overwrite = false)
        {
            this.localMachineManager.SendFile(filePathOnTargetMachine, fileContents, appended, overwrite);
        }

        /// <inheritdoc />
        public byte[] RetrieveFile(string filePathOnTargetMachine)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public ICollection<dynamic> RunScript(string scriptBlock, ICollection<object> scriptBlockParameters = null)
        {
            var parameters = string.Join(",", scriptBlockParameters ?? new string[0]);
            var content = parameters + Environment.NewLine + scriptBlock;
            File.WriteAllText(this.GetPath(), content);
            return new dynamic[0];
        }

        private string GetPath(string note = null)
        {
            var noteAddIn = string.IsNullOrWhiteSpace(note) ? string.Empty : "-" + note;
            this.index = this.index + 1;
            var result = Path.Combine(this.outputPath, Invariant($"{this.groupIndex}-{this.index}{noteAddIn}.ps1"));
            return result;
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "localMachineManager", Justification = "Is disposed correctly.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Justification = "Is disposed correctly.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Is disposed correctly.")]
        public void Dispose()
        {
            this.localMachineManager?.Dispose();
        }
    }
}
