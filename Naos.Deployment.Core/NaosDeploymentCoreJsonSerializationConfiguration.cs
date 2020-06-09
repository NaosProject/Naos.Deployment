// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NaosDeploymentCoreJsonSerializationConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Tracking;
    using OBeautifulCode.Representation.System;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Json;

    /// <summary>
    /// Serialization configuration.
    /// </summary>
    public class NaosDeploymentCoreJsonSerializationConfiguration : JsonSerializationConfigurationBase
    {
        /// <inheritdoc />
        public override UnregisteredTypeEncounteredStrategy UnregisteredTypeEncounteredStrategy => UnregisteredTypeEncounteredStrategy.Attempt;

        /// <inheritdoc />
        protected override IReadOnlyCollection<JsonSerializationConfigurationType> DependentJsonSerializationConfigurationTypes =>
            new[]
            {
                typeof(NaosDeploymentDomainJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
                typeof(NaosDeploymentTrackingJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
            };

        /// <inheritdoc />
        protected override IReadOnlyCollection<TypeToRegisterForJson> TypesToRegisterForJson => new[]
        {
            typeof(PackageWithBundleIdentifier).ToTypeToRegisterForJson(),
            typeof(PackagedDeploymentConfiguration).ToTypeToRegisterForJson(),
            typeof(MessageBusHandlerHarnessConfiguration).ToTypeToRegisterForJson(),
            typeof(CertificateManagementConfigurationBase).ToTypeToRegisterForJson(),
            typeof(AdjustDeploymentBase).ToTypeToRegisterForJson(),
        };

        /// <summary>
        /// Gets the default serializer representation.
        /// </summary>
        /// <value>The default serializer representation.</value>
        public static SerializerRepresentation NaosDeploymentCoreJsonSerializerRepresentation => new SerializerRepresentation(
            SerializationKind.Json,
            typeof(NaosDeploymentCoreJsonSerializationConfiguration).ToRepresentation());
    }
}
