// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentJsonConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.Collections.Generic;
    using Naos.Cron.Serialization.Json;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;
    using Naos.Packaging.Serialization.Json;
    using Naos.Serialization.Json;

    /// <summary>
    /// <see cref="JsonConfigurationBase" /> implementation for Deployment types.
    /// </summary>
    public class DeploymentJsonConfiguration : JsonConfigurationBase
    {
        /// <inheritdoc />
        public override IReadOnlyCollection<Type> DependentConfigurationTypes =>
            new[]
            {
                typeof(CronJsonConfiguration),
                typeof(LoggingJsonConfiguration),
                typeof(PackagingJsonConfiguration),
                typeof(MessageBusJsonConfiguration),
            };

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Expected for this type of configuration.")]
        protected override IReadOnlyCollection<Type> TypesToAutoRegister =>
            new[]
            {
                typeof(ArcologyInfo),
                typeof(AutoStartProvider),
                typeof(CertificateDescription),
                typeof(CertificateDescriptionWithClearPfxPayload),
                typeof(CertificateDescriptionWithEncryptedPfxPayload),
                typeof(CertificateLocator),
                typeof(ComputingContainerDescription),
                typeof(ComputingInfrastructureManagerSettings),
                typeof(Create),
                typeof(DatabaseMigrationBase),
                typeof(DatabaseRestoreBase),
                typeof(DeployedInstance),
                typeof(DeploymentConfiguration),
                typeof(DeploymentConfigurationWithStrategies),
                typeof(DeploymentException),
                typeof(DeploymentStrategy),
                typeof(DirectoryToCreateDetails),
                typeof(Encryptor),
                typeof(ImageDetails),
                typeof(InitializationStrategyBase),
                typeof(InstanceCreationDetails),
                typeof(InstanceDescription),
                typeof(InstanceDetailsFromComputingPlatform),
                typeof(InstanceStatus),
                typeof(InstanceTargeterBase),
                typeof(InstanceType),
                typeof(ItsConfigOverride),
                typeof(OperatingSystemDescriptionBase),
                typeof(PackageDescriptionWithDeploymentStatus),
                typeof(PackageDescriptionWithOverrides),
                typeof(PostDeploymentStrategy),
                typeof(SetupStepFactorySettings),
                typeof(Volume),
            };
    }
}
