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
    using Naos.Packaging.Domain;
    using Naos.Serialization.Bson;

    /// <summary>
    /// Register class mapping necessary for the StorageModel.
    /// </summary>
    public class DeploymentBsonConfiguration : BsonConfigurationBase
    {
        /// <inheritdoc cref="BsonConfigurationBase" />
        protected override IReadOnlyCollection<Type> TypesToAutoRegister => new[]
                                                                                {
                                                                                    typeof(ComputingContainerDescription),
                                                                                    typeof(ArcologyInfo),
                                                                                    typeof(PackageDescription),
                                                                                    typeof(PackageDescriptionWithDeploymentStatus),
                                                                                    typeof(InstanceDescription),
                                                                                    typeof(InstanceType),
                                                                                    typeof(Volume),
                                                                                    typeof(DeploymentConfiguration),
                                                                                    typeof(CertificateDescription),
                                                                                    typeof(CertificateLocator),
                                                                                    typeof(CertificateDescriptionWithClearPfxPayload),
                                                                                    typeof(CertificateDescriptionWithEncryptedPfxPayload),
                                                                                    typeof(CertificateContainer),
                                                                                };
    }
}