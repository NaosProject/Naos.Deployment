// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BsonClassMapManager.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Persistence
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    using MongoDB.Bson.Serialization;
    using MongoDB.Bson.Serialization.Options;
    using MongoDB.Bson.Serialization.Serializers;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    using OBeautifulCode.DateTime;

    /// <summary>
    /// Register class mapping necessary for the StorageModel.
    /// </summary>
    public static class BsonClassMapManager
    {
        private static readonly object SyncRegister = new object();

        private static bool registered = false;

        /// <summary>
        /// Class to manage class maps necessary for the CoScore Storage Model.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        public static void RegisterClassMaps()
        {
            if (!registered)
            {
                lock (SyncRegister)
                {
                    if (!registered)
                    {
                        BsonClassMap.RegisterClassMap<ComputingContainerDescription>(
                            cm =>
                                {
                                    cm.AutoMap();
                                    cm.MapMember(c => c.InstanceAccessibility)
                                        .SetSerializer(
                                            new EnumSerializer<InstanceAccessibility>(MongoDB.Bson.BsonType.String));
                                });

                        BsonClassMap.RegisterClassMap<ArcologyInfo>(
                            cm =>
                                {
                                    cm.AutoMap();
                                    cm.MapMember(c => c.RootDomainHostingIdMap)
                                        .SetSerializer(
                                            new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(
                                                DictionaryRepresentation.ArrayOfDocuments));
                                    cm.MapMember(c => c.WindowsSkuSearchPatternMap)
                                        .SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<WindowsSku, string>>(DictionaryRepresentation.ArrayOfDocuments, new EnumSerializer<WindowsSku>(MongoDB.Bson.BsonType.String), new ObjectSerializer()));
                                });

                        BsonClassMap.RegisterClassMap<PackageDescription>();
                        BsonClassMap.RegisterClassMap<PackageDescriptionWithDeploymentStatus>(
                            cm =>
                                {
                                    cm.AutoMap();
                                    cm.MapMember(c => c.DeploymentStatus)
                                        .SetSerializer(
                                            new EnumSerializer<PackageDeploymentStatus>(MongoDB.Bson.BsonType.String));
                                });

                        BsonClassMap.RegisterClassMap<InstanceDescription>(
                            cm =>
                                {
                                    cm.AutoMap();
                                    cm.MapMember(c => c.DeployedPackages)
                                        .SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, PackageDescriptionWithDeploymentStatus>>(DictionaryRepresentation.ArrayOfDocuments));
                                    cm.MapMember(c => c.SystemSpecificDetails)
                                        .SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(DictionaryRepresentation.ArrayOfDocuments));
                                });

                        BsonClassMap.RegisterClassMap<InstanceType>(
                            cm =>
                                {
                                    cm.AutoMap();
                                    cm.MapMember(c => c.WindowsSku)
                                        .SetSerializer(
                                            new EnumSerializer<WindowsSku>(MongoDB.Bson.BsonType.String));
                                });

                        BsonClassMap.RegisterClassMap<Volume>(
                            cm =>
                                {
                                    cm.AutoMap();
                                    cm.MapMember(c => c.Type)
                                        .SetSerializer(
                                            new EnumSerializer<VolumeType>(MongoDB.Bson.BsonType.String));
                                });

                        BsonClassMap.RegisterClassMap<DeploymentConfiguration>(
                            cm =>
                                {
                                    cm.AutoMap();
                                    cm.MapMember(c => c.InstanceAccessibility)
                                        .SetSerializer(
                                            new EnumSerializer<InstanceAccessibility>(MongoDB.Bson.BsonType.String));
                                });

                        BsonClassMap.RegisterClassMap<CertificateDescription>(
                            cm =>
                                {
                                    cm.AutoMap();
                                    cm.MapMember(c => c.CertificateAttributes)
                                        .SetSerializer(
                                            new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(DictionaryRepresentation.ArrayOfDocuments));
                                });

                        BsonClassMap.RegisterClassMap<CertificateLocator>(
                            cm =>
                                {
                                    cm.AutoMap();
                                    cm.MapMember(c => c.CertificateStoreName).SetSerializer(new EnumSerializer<StoreName>(MongoDB.Bson.BsonType.String));
                                    cm.MapMember(c => c.CertificateStoreLocation).SetSerializer(new EnumSerializer<StoreLocation>(MongoDB.Bson.BsonType.String));
                                });

                        BsonClassMap.RegisterClassMap<CertificateDescriptionWithClearPfxPayload>();
                        BsonClassMap.RegisterClassMap<CertificateDescriptionWithEncryptedPfxPayload>();

                        BsonClassMap.RegisterClassMap<CertificateContainer>();

                        registered = true;
                    }
                }
            }
        }
    }
}