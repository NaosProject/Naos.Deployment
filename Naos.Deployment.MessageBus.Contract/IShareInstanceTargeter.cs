// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IShareInstanceTargeter.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Contract
{
    using Naos.Deployment.Domain;
    using Naos.MessageBus.DataContract;

    /// <summary>
    /// Interface to support sharing the object being used to target the instance
    /// </summary>
    public interface IShareInstanceTargeter : IShare
    {
        /// <summary>
        /// Gets or sets the targeter to find an instance.
        /// </summary>
        InstanceTargeterBase[] InstanceTargeters { get; set; }
    }
}
