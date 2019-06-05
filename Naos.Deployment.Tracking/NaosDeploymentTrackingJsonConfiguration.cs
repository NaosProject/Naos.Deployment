// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NaosDeploymentTrackingJsonConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System;
    using System.Collections.Generic;
    using Naos.Deployment.Domain;
    using Naos.Serialization.Json;

    /// <summary>
    /// Serialization configuration.
    /// </summary>
    public class NaosDeploymentTrackingJsonConfiguration : JsonConfigurationBase
    {
        /// <inheritdoc />
        public override IReadOnlyCollection<Type> DependentConfigurationTypes =>
            new[] { typeof(NaosDeploymentDomainJsonConfiguration) };

        /// <inheritdoc />
        protected override IReadOnlyCollection<Type> TypesToAutoRegister => new[]
        {
            typeof(InfrastructureTrackerConfigurationBase),
        };
    }
}