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
    using Naos.MachineManagement.Domain;
    using OBeautifulCode.Representation.System;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Json;

    /// <summary>
    /// Serialization configuration.
    /// </summary>
    public class NaosDeploymentCoreJsonSerializationConfiguration : JsonSerializationConfigurationBase
    {
        /// <inheritdoc />
        protected override IReadOnlyCollection<string> TypeToRegisterNamespacePrefixFilters => new[]
                                                                                               {
                                                                                                   this.GetType().Namespace,
                                                                                                   typeof(MachineProtocol).Namespace, // this is a hack and we should have a specific serialization config for Naos.MachineManagement in the future.
                                                                                               };

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
            typeof(PackagedDeploymentConfiguration).ToTypeToRegisterForJson(),
            typeof(PackageWithBundleIdentifier).ToTypeToRegisterForJson(),
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
