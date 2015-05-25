// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategy.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Strategy to initialize the application.
    /// </summary>
    [KnownType(typeof(InitializationStrategyConsole))]
    [KnownType(typeof(InitializationStrategyDatabase))]
    [KnownType(typeof(InitializationStrategyWeb))]
    public abstract class InitializationStrategy
    {
    }
}
