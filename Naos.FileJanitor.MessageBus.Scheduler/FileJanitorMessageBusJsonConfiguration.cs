// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileJanitorMessageBusJsonConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using System;
    using System.Collections.Generic;
    using Naos.MessageBus.Domain;
    using Naos.Serialization.Json;

    /// <summary>
    /// Serialization configuration.
    /// </summary>
    public class FileJanitorMessageBusJsonConfiguration : JsonConfigurationBase
    {
        /// <inheritdoc />
        public override IReadOnlyCollection<Type> DependentConfigurationTypes =>
            new[] { typeof(MessageBusJsonConfiguration) };

        /// <inheritdoc />
        protected override IReadOnlyCollection<Type> TypesToAutoRegister => new[]
        {
            typeof(FileLocationAffectedItem),
        };
    }
}
