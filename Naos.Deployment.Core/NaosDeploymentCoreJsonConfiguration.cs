// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NaosDeploymentCoreJsonConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Tracking;
    using Naos.Serialization.Json;

    /// <summary>
    /// Serialization configuration.
    /// </summary>
    public class NaosDeploymentCoreJsonConfiguration : JsonConfigurationBase
    {
        /// <inheritdoc />
        public override IReadOnlyCollection<Type> DependentConfigurationTypes =>
            new[]
            {
                typeof(NaosDeploymentDomainJsonConfiguration),
                typeof(NaosDeploymentTrackingJsonConfiguration),
            };

        /// <inheritdoc />
        protected override IReadOnlyCollection<Type> TypesToAutoRegister => new[]
        {
            typeof(PackageWithBundleIdentifier),
            typeof(PackagedDeploymentConfiguration),
            typeof(MessageBusHandlerHarnessConfiguration),
            typeof(CertificateManagementConfigurationBase),
        };
    }
}