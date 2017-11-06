// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentBsonConfiguration.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Persistence
{
    using System;
    using System.Collections.Generic;

    using Naos.Deployment.Domain;
    using Naos.Serialization.Bson;

    /// <summary>
    /// Register class mapping necessary for the StorageModel.
    /// </summary>
    public class DeploymentBsonConfiguration : BsonConfigurationBase
    {
        /// <inheritdoc cref="BsonConfigurationBase" />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Has a lot of type references by its nature.")]
        protected override IReadOnlyCollection<Type> TypesToAutoRegister => new[]
                                                                                {
                                                                                    // Domain types
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
                                                                                    typeof(InitializationStrategyCertificateToInstall),
                                                                                    typeof(InitializationStrategyCreateEventLog),
                                                                                    typeof(InitializationStrategyDirectoryToCreate),
                                                                                    typeof(InitializationStrategyDnsEntry),
                                                                                    typeof(InitializationStrategyIis),
                                                                                    typeof(InitializationStrategyMessageBusHandler),
                                                                                    typeof(InitializationStrategyMongo),
                                                                                    typeof(InitializationStrategyScheduledTask),
                                                                                    typeof(InitializationStrategySelfHost),
                                                                                    typeof(InitializationStrategySqlServer),
                                                                                    typeof(InstanceCreationDetails),
                                                                                    typeof(InstanceDescription),
                                                                                    typeof(InstanceDetailsFromComputingPlatform),
                                                                                    typeof(InstanceStatus),
                                                                                    typeof(InstanceTargeterBase),
                                                                                    typeof(InstanceType),
                                                                                    typeof(ItsConfigOverride),
                                                                                    typeof(PackageDescriptionWithDeploymentStatus),
                                                                                    typeof(PackageDescriptionWithOverrides),
                                                                                    typeof(PostDeploymentStrategy),
                                                                                    typeof(SetupStepFactorySettings),
                                                                                    typeof(Volume),

                                                                                    // Persisitence types
                                                                                    typeof(ArcologyInfoContainer),
                                                                                    typeof(CertificateContainer),
                                                                                    typeof(InstanceContainer),
                                                                                };
    }
}