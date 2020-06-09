// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NaosDeploymentTrackingJsonSerializationConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System.Collections.Generic;
    using Naos.Deployment.Domain;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Json;

    /// <summary>
    /// Serialization configuration.
    /// </summary>
    public class NaosDeploymentTrackingJsonSerializationConfiguration : JsonSerializationConfigurationBase
    {
        /// <inheritdoc />
        public override UnregisteredTypeEncounteredStrategy UnregisteredTypeEncounteredStrategy => UnregisteredTypeEncounteredStrategy.Attempt;

        /// <inheritdoc />
        protected override IReadOnlyCollection<JsonSerializationConfigurationType> DependentJsonSerializationConfigurationTypes =>
            new[]
            {
                typeof(NaosDeploymentDomainJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
            };

        /// <inheritdoc />
        protected override IReadOnlyCollection<TypeToRegisterForJson> TypesToRegisterForJson => new[]
        {
            typeof(InfrastructureTrackerConfigurationBase).ToTypeToRegisterForJson(),
        };
    }
}
