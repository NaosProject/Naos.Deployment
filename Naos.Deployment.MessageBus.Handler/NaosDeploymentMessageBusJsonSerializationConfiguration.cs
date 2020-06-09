// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NaosDeploymentMessageBusJsonSerializationConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Collections.Generic;
    using Naos.Deployment.Tracking;
    using OBeautifulCode.Representation.System;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Json;
    using static System.FormattableString;

    /// <summary>
    /// MessageBus serialization configuration.
    /// </summary>
    public class NaosDeploymentMessageBusJsonSerializationConfiguration : JsonSerializationConfigurationBase
    {
        /// <inheritdoc />
        public override UnregisteredTypeEncounteredStrategy UnregisteredTypeEncounteredStrategy => UnregisteredTypeEncounteredStrategy.Attempt;

        /// <inheritdoc />
        protected override IReadOnlyCollection<JsonSerializationConfigurationType> DependentJsonSerializationConfigurationTypes =>
            new[]
            {
                typeof(NaosDeploymentTrackingJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
            };

        /// <inheritdoc />
        protected override IReadOnlyCollection<TypeToRegisterForJson> TypesToRegisterForJson => new[]
                                                                                                {
                                                                                                    typeof(DeploymentMessageHandlerSettings)
                                                                                                       .ToTypeToRegisterForJson(),
                                                                                                };

        /// <summary>
        /// Gets the default serializer representation.
        /// </summary>
        /// <value>The default serializer representation.</value>
        public static SerializerRepresentation NaosDeploymentMessageBusJsonSerializerRepresentation
            => new SerializerRepresentation(
                SerializationKind.Json,
                typeof(NaosDeploymentMessageBusJsonSerializationConfiguration).ToRepresentation());
    }
}
