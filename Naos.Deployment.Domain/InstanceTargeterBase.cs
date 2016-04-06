// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceTargeterBase.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.ComponentModel;

    /// <summary>
    /// Model class to contain information to find an instance.
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public class InstanceTargeterBase
    {
    }

    /// <summary>
    /// Implementation of InstanceTargeterBase that uses a specific ID from the computing platform provider.
    /// </summary>
    public class InstanceTargeterSystemId : InstanceTargeterBase
    {
        /// <summary>
        /// Gets or sets the ID (per the computing platform provider) of the instance to change the type of.
        /// </summary>
        public string InstanceId { get; set; }
    }

    /// <summary>
    /// Implementation of InstanceTargeterBase that uses a name to be looked.
    /// </summary>
    public class InstanceTargeterNameLookup : InstanceTargeterBase
    {
        /// <summary>
        /// Gets or sets the instance name.
        /// </summary>
        public string InstanceName { get; set; }
    }
}
