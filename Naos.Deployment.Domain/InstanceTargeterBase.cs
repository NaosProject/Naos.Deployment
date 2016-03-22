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
    /// Implementation of InstanceTargeterBase that uses a specific ID from the cloud provider.
    /// </summary>
    public class InstanceTargeterSystemId : InstanceTargeterBase
    {
        /// <summary>
        /// Gets or sets the ID (per the cloud provider) of the instance to change the type of.
        /// </summary>
        public string InstanceId { get; set; }
    }

    /// <summary>
    /// Implementation of InstanceTargeterBase that uses a name to be looked via the instance tags ON the cloud provider.
    /// </summary>
    public class InstanceTargeterNameLookupByCloudTag : InstanceTargeterBase
    {
        /// <summary>
        /// Gets or sets the instance name.
        /// </summary>
        public string InstanceNameInTag { get; set; }
    }
}
