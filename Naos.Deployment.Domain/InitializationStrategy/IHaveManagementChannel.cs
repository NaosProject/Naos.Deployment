// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHaveManagementChannel.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
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