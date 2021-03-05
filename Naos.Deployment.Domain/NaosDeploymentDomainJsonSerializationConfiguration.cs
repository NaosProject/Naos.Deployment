// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NaosDeploymentDomainJsonSerializationConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;
    using Naos.Cron.Serialization.Json;
    using Naos.Database.Serialization.Json;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;
    using Naos.Mongo.Serialization.Json;
    using Naos.Packaging.Serialization.Json;
    using Naos.SqlServer.Serialization.Json;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Json;

    /// <summary>
    /// <see cref="JsonSerializationConfigurationBase" /> implementation for Deployment types.
    /// </summary>
    public class NaosDeploymentDomainJsonSerializationConfiguration : JsonSerializationConfigurationBase
    {
        /// <inheritdoc />
        public override UnregisteredTypeEncounteredStrategy UnregisteredTypeEncounteredStrategy => UnregisteredTypeEncounteredStrategy.Attempt;

        /// <inheritdoc />
        protected override IReadOnlyCollection<JsonSerializationConfigurationType> DependentJsonSerializationConfigurationTypes =>
            new[]
            {
                typeof(CronJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
                typeof(LoggingJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
                typeof(PackagingJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
                typeof(MessageBusJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
                typeof(DatabaseJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
                typeof(MongoJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
                typeof(SqlServerJsonSerializationConfiguration).ToJsonSerializationConfigurationType(),
            };

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Expected for this type of configuration.")]
        protected override IReadOnlyCollection<TypeToRegisterForJson> TypesToRegisterForJson =>
            new[]
            {
                typeof(ArcologyInfo).ToTypeToRegisterForJson(),
                typeof(AutoStartProvider).ToTypeToRegisterForJson(),
                typeof(CertificateDescription).ToTypeToRegisterForJson(),
                typeof(CertificateDescriptionWithClearPfxPayload).ToTypeToRegisterForJson(),
                typeof(CertificateDescriptionWithEncryptedPfxPayload).ToTypeToRegisterForJson(),
                typeof(CertificateLocator).ToTypeToRegisterForJson(),
                typeof(ComputingContainerDescription).ToTypeToRegisterForJson(),
                typeof(ComputingInfrastructureManagerSettings).ToTypeToRegisterForJson(),
                typeof(Create).ToTypeToRegisterForJson(),
                typeof(DatabaseRestoreBase).ToTypeToRegisterForJson(),
                typeof(DeployedInstance).ToTypeToRegisterForJson(),
                typeof(DeploymentConfiguration).ToTypeToRegisterForJson(),
                typeof(DeploymentConfigurationWithStrategies).ToTypeToRegisterForJson(),
                typeof(DeploymentException).ToTypeToRegisterForJson(),
                typeof(DeploymentStrategy).ToTypeToRegisterForJson(),
                typeof(DirectoryToCreateDetails).ToTypeToRegisterForJson(),
                typeof(Encryptor).ToTypeToRegisterForJson(),
                typeof(ImageDetails).ToTypeToRegisterForJson(),
                typeof(InitializationStrategyBase).ToTypeToRegisterForJson(),
                typeof(InstanceCreationDetails).ToTypeToRegisterForJson(),
                typeof(InstanceDescription).ToTypeToRegisterForJson(),
                typeof(InstanceDetailsFromComputingPlatform).ToTypeToRegisterForJson(),
                typeof(InstanceStatus).ToTypeToRegisterForJson(),
                typeof(InstanceTargeterBase).ToTypeToRegisterForJson(),
                typeof(InstanceType).ToTypeToRegisterForJson(),
                typeof(ItsConfigOverride).ToTypeToRegisterForJson(),
                typeof(OperatingSystemDescriptionBase).ToTypeToRegisterForJson(),
                typeof(PackageDescriptionWithDeploymentStatus).ToTypeToRegisterForJson(),
                typeof(PackageDescriptionWithOverrides).ToTypeToRegisterForJson(),
                typeof(PostDeploymentStrategy).ToTypeToRegisterForJson(),
                typeof(SetupStepFactorySettings).ToTypeToRegisterForJson(),
                typeof(Volume).ToTypeToRegisterForJson(),
            };
    }
}
