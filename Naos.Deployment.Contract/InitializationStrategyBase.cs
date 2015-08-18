// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyBase.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.ComponentModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Strategy to initialize the application.
    /// </summary>
    [KnownType(typeof(InitializationStrategyMessageBusHandler))]
    [KnownType(typeof(InitializationStrategySqlServer))]
    [KnownType(typeof(InitializationStrategyIis))]
    [KnownType(typeof(InitializationStrategyPrivateDnsEntry))]
    [KnownType(typeof(InitializationStrategyDirectoryToCreate))]
    [KnownType(typeof(InitializationStrategyCertificateToInstall))]
    [Bindable(BindableSupport.Default)]
    public abstract class InitializationStrategyBase
    {
    }
}
