// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyBase.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Strategy to initialize the application.
    /// </summary>
    [KnownType(typeof(InitializationStrategyMessageBusHandler))]
    [KnownType(typeof(InitializationStrategySqlServer))]
    [KnownType(typeof(InitializationStrategyMongo))]
    [KnownType(typeof(InitializationStrategyIis))]
    [KnownType(typeof(InitializationStrategyDnsEntry))]
    [KnownType(typeof(InitializationStrategyDirectoryToCreate))]
    [KnownType(typeof(InitializationStrategyCertificateToInstall))]
    [Bindable(BindableSupport.Default)]
    public abstract class InitializationStrategyBase : ICloneable
    {
        /// <summary>
        /// Clone method to duplicate the strategy in a way that can be used without damaging the original copy.
        /// </summary>
        /// <returns>A deeply clone duplicate of the object.</returns>
        public abstract object Clone();
    }
}
