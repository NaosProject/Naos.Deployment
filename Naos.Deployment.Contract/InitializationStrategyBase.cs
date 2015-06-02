// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyBase.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Strategy to initialize the application.
    /// </summary>
    [KnownType(typeof(InitializationStrategyMessageBusHandler))]
    [KnownType(typeof(InitializationStrategyDatabase))]
    [KnownType(typeof(InitializationStrategyWeb))]
    public abstract class InitializationStrategyBase
    {
    }
}
