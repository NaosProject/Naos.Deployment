// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileJanitorMessageBusJsonSerializationConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.FileJanitor.MessageBus.Scheduler
{
    using System;
    using System.Collections.Generic;
    using Naos.MessageBus.Domain;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Json;

    /// <summary>
    /// Serialization configuration.
    /// </summary>
    public class FileJanitorMessageBusJsonSerializationConfiguration : JsonSerializationConfigurationBase
    {
        /// <inheritdoc />
        public override UnregisteredTypeEncounteredStrategy UnregisteredTypeEncounteredStrategy => UnregisteredTypeEncounteredStrategy.Attempt;

        /// <inheritdoc />
        protected override IReadOnlyCollection<JsonSerializationConfigurationType> DependentJsonSerializationConfigurationTypes =>
            new[]
            {
                typeof(MessageBusJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
            };

        /// <inheritdoc />
        protected override IReadOnlyCollection<TypeToRegisterForJson> TypesToRegisterForJson => new[]
        {
            typeof(FileLocationAffectedItem).ToTypeToRegisterForJson(),
        };
    }
}
