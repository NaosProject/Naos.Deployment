// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NaosDeploymentMessageBusJsonConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Collections.Generic;
    using Naos.Deployment.Tracking;
    using OBeautifulCode.Serialization.Json;
    using static System.FormattableString;

    /// <summary>
    /// MessageBus serialization configuration.
    /// </summary>
    public class NaosDeploymentMessageBusJsonConfiguration : JsonConfigurationBase
    {
        /// <inheritdoc />
        public override IReadOnlyCollection<Type> DependentConfigurationTypes =>
            new[] { typeof(NaosDeploymentTrackingJsonConfiguration) };

        /// <inheritdoc />
        protected override IReadOnlyCollection<Type> TypesToAutoRegister => new[]
        {
            typeof(DeploymentMessageHandlerSettings),
        };
    }
}
