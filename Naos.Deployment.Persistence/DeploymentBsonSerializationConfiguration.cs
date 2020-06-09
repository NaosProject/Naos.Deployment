// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentBsonSerializationConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Persistence
{
    using System;
    using System.Collections.Generic;

    using Naos.Cron.Serialization.Bson;
    using Naos.Deployment.Domain;
    using Naos.Packaging.Serialization.Bson;
    using OBeautifulCode.Serialization.Bson;

    /// <summary>
    /// Register class mapping necessary for the StorageModel.
    /// </summary>
    public class DeploymentBsonSerializationConfiguration : BsonSerializationConfigurationBase
    {
        /// <inheritdoc />
        protected override IReadOnlyCollection<BsonSerializationConfigurationType> DependentBsonSerializationConfigurationTypes =>
            new[]
            {
                typeof(CronBsonSerializationConfiguration).ToBsonSerializationConfigurationType(),
                typeof(PackagingBsonSerializationConfiguration).ToBsonSerializationConfigurationType(),
            };

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Has a lot of type references by its nature.")]
        protected override IReadOnlyCollection<TypeToRegisterForBson> TypesToRegisterForBson =>
                                                                                new[]
                                                                                {
                                                                                    // Domain types
                                                                                    typeof(ArcologyInfo).ToTypeToRegisterForBson(),
                                                                                    typeof(AutoStartProvider).ToTypeToRegisterForBson(),
                                                                                    typeof(CertificateDescription).ToTypeToRegisterForBson(),
                                                                                    typeof(CertificateDescriptionWithClearPfxPayload).ToTypeToRegisterForBson(),
                                                                                    typeof(CertificateDescriptionWithEncryptedPfxPayload).ToTypeToRegisterForBson(),
                                                                                    typeof(CertificateLocator).ToTypeToRegisterForBson(),
                                                                                    typeof(ComputingContainerDescription).ToTypeToRegisterForBson(),
                                                                                    typeof(ComputingInfrastructureManagerSettings).ToTypeToRegisterForBson(),
                                                                                    typeof(Create).ToTypeToRegisterForBson(),
                                                                                    typeof(DatabaseMigrationBase).ToTypeToRegisterForBson(),
                                                                                    typeof(DatabaseRestoreBase).ToTypeToRegisterForBson(),
                                                                                    typeof(DeployedInstance).ToTypeToRegisterForBson(),
                                                                                    typeof(DeploymentConfiguration).ToTypeToRegisterForBson(),
                                                                                    typeof(DeploymentConfigurationWithStrategies).ToTypeToRegisterForBson(),
                                                                                    typeof(DeploymentException).ToTypeToRegisterForBson(),
                                                                                    typeof(DeploymentStrategy).ToTypeToRegisterForBson(),
                                                                                    typeof(DirectoryToCreateDetails).ToTypeToRegisterForBson(),
                                                                                    typeof(Encryptor).ToTypeToRegisterForBson(),
                                                                                    typeof(ImageDetails).ToTypeToRegisterForBson(),
                                                                                    typeof(InitializationStrategyBase).ToTypeToRegisterForBson(),
                                                                                    typeof(InstanceCreationDetails).ToTypeToRegisterForBson(),
                                                                                    typeof(InstanceDescription).ToTypeToRegisterForBson(),
                                                                                    typeof(InstanceDetailsFromComputingPlatform).ToTypeToRegisterForBson(),
                                                                                    typeof(InstanceStatus).ToTypeToRegisterForBson(),
                                                                                    typeof(InstanceTargeterBase).ToTypeToRegisterForBson(),
                                                                                    typeof(InstanceType).ToTypeToRegisterForBson(),
                                                                                    typeof(ItsConfigOverride).ToTypeToRegisterForBson(),
                                                                                    typeof(OperatingSystemDescriptionBase).ToTypeToRegisterForBson(),
                                                                                    typeof(PackageDescriptionWithDeploymentStatus).ToTypeToRegisterForBson(),
                                                                                    typeof(PackageDescriptionWithOverrides).ToTypeToRegisterForBson(),
                                                                                    typeof(PostDeploymentStrategy).ToTypeToRegisterForBson(),
                                                                                    typeof(SetupStepFactorySettings).ToTypeToRegisterForBson(),
                                                                                    typeof(Volume).ToTypeToRegisterForBson(),

                                                                                    // Persistence types
                                                                                    typeof(ArcologyInfoContainer).ToTypeToRegisterForBson(),
                                                                                    typeof(CertificateContainer).ToTypeToRegisterForBson(),
                                                                                    typeof(InstanceContainer).ToTypeToRegisterForBson(),
                                                                                };
    }
}
