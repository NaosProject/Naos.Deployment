// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHaveManagementChannel.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Interface to expose the management channel name.
    /// </summary>
    public interface IHaveManagementChannel
    {
        /// <summary>
        /// Gets the channel name to monitor management commands.
        /// </summary>
        string ManagementChannelName { get; }
    }
}