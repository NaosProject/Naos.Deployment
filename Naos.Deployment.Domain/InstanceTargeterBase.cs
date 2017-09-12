// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceTargeterBase.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
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
        /// Initializes a new instance of the <see cref="InstanceTargeterSystemId"/> class.
        /// </summary>
        /// <param name="instanceId">ID of instance to target.</param>
        public InstanceTargeterSystemId(string instanceId)
        {
            this.InstanceId = instanceId;
        }

        /// <summary>
        /// Gets the ID (per the computing platform provider) of the instance to change the type of.
        /// </summary>
        public string InstanceId { get; private set; }
    }

    /// <summary>
    /// Implementation of InstanceTargeterBase that uses a name to be looked.
    /// </summary>
    public class InstanceTargeterNameLookup : InstanceTargeterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceTargeterNameLookup"/> class.
        /// </summary>
        /// <param name="name">Name to lookup in the arcology to find the instance to target (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a).</param>
        public InstanceTargeterNameLookup(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the instance (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a)'.
        /// </summary>
        public string Name { get; private set; }
    }
}
