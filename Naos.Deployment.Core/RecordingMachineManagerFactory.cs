// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RecordingMachineManagerFactory.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using Naos.MachineManagement.Domain;

    /// <summary>
    /// Interface of the factory to create <see cref="IManageMachines" />.
    /// </summary>
    public class RecordingMachineManagerFactory : ICreateMachineManagers
    {
        /// <inheritdoc />
        public IManageMachines CreateMachineManager(MachineProtocol machineProtocol, string address, string userName, string password)
        {
            throw new System.NotImplementedException();
        }
    }
}