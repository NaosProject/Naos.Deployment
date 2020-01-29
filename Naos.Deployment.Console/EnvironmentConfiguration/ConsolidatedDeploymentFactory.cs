// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsolidatedDeploymentFactory.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;

    using Naos.Cron;
    using Naos.Database.Domain;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.Domain;
    using Naos.Packaging.Domain;

    using OBeautifulCode.Type;
    using OBeautifulCode.Validation.Recipes;

    using Spritely.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Consolidated configurations for package repositories.
    /// </summary>
    public static class ConsolidatedDeploymentFactory
    {
        private const string NullPackageId = "Naos.Packaging.Null";

        private const string AdhocDeploymentTagKey = "DeployedAdhoc";

        /// <summary>
        /// Builds an adhoc machine deployment.
        /// </summary>
        /// <param name="instanceName">Name of the instance.</param>
        /// <param name="instanceType">Optionally set a system specific type of the instance.</param>
        /// <returns>Deployment to use.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Adhoc", Justification = "Spelling/name is correct.")]
        public static ConsolidatedDeployment BuildAdhocDeployment(string instanceName, string instanceType)
        {
            var packages = new[] { new PackageDescriptionWithOverrides { PackageDescription = new PackageDescription { Id = NullPackageId, }, }, };
            var instanceTypeLocal = string.IsNullOrWhiteSpace(instanceType)
                                        ? null
                                        : new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.DoesNotMatter }, SpecificInstanceTypeSystemId = instanceType, };

            var deploymentConfigurationOverride = new DeploymentConfiguration
            {
                InstanceType = instanceTypeLocal,
                TagNameToValueMap = new Dictionary<string, string> { { AdhocDeploymentTagKey, true.ToString() }, },
            };

            var ret = new ConsolidatedDeployment
            {
                Name = instanceName,
                Packages = packages,
                DeploymentConfigurationOverride = deploymentConfigurationOverride,
            };

            return ret;
        }

        /// <summary>
        /// Builds VPN Server deployment.
        /// </summary>
        /// <param name="locationName">Will be the region name for AWS.</param>
        /// <returns>Deployment to use.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Vpn", Justification = "Spelling/name is correct.")]
        public static ConsolidatedDeployment BuildVpnServerDeployment(string locationName)
        {
            string openVpnImageIdFromMarketPlace;
            switch (locationName)
            {
                case "us-east-1":
                    openVpnImageIdFromMarketPlace = "ami-0ca1c6f31c3fb1708";
                    break;
                default:
                    throw new NotSupportedException("Location/region is not supported: " + locationName);
            }

            var packages = new[]
            {
                new PackageDescriptionWithOverrides
                {
                    PackageDescription = new PackageDescription { Id = NullPackageId, },
                },
            };

            return new ConsolidatedDeployment
                   {
                       Name = "OpenVpn",
                       Packages = packages,
                       DeploymentConfigurationOverride = new DeploymentConfigurationWithStrategies
                                                         {
                                                             InstanceType = new InstanceType
                                                                            {
                                                                                SpecificInstanceTypeSystemId = "t2.small",
                                                                                OperatingSystem = new OperatingSystemDescriptionLinux
                                                                                                  {
                                                                                                      Distribution = LinuxDistribution
                                                                                                         .SpecificImageSupplied,
                                                                                                  },
                                                                                SpecificImageSystemId = openVpnImageIdFromMarketPlace,
                                                                            },
                                                             InitializationStrategies = new[]
                                                                                        {
                                                                                            new InitializationStrategyDnsEntry
                                                                                            {
                                                                                                PrivateDnsEntry = "vpn.{environment}.naosproject.com",
                                                                                            },
                                                                                        },
                                                         },
                   };
        }

        /// <summary>
        /// Builds Arcology database server.
        /// </summary>
        /// <param name="administratorPassword">Database administrator password.</param>
        /// <returns>Deployment to use.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arcology", Justification = "Spelling/name is correct.")]
        public static ConsolidatedDeployment BuildArcologyServerDeployment(string administratorPassword)
        {
            var packages = new[]
                               {
                                   new PackageDescriptionWithOverrides
                                       {
                                           PackageDescription = new PackageDescription { Id = "Naos.Deployment.Persistence", },
                                           InitializationStrategies =
                                               new InitializationStrategyBase[]
                                                   {
                                                       new InitializationStrategyMongo
                                                           {
                                                               AdministratorPassword = administratorPassword,
                                                               DocumentDatabaseName = "Deployment",
                                                               ManagementChannelName = "deployment",
                                                           },
                                                       new InitializationStrategyDnsEntry
                                                           {
                                                               PrivateDnsEntry = "deployment.database.{environment}.naosproject.com",
                                                           },
                                                   },
                                       },
                               };

            var deploymentConfigurationOverride = new DeploymentConfiguration
            {
                InstanceType =
                                                              new InstanceType
                                                              {
                                                                  SpecificInstanceTypeSystemId = "t2.small",
                                                                  OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.Base },
                                                              },
                Volumes = new[]
                                                                        {
                                                                            new Volume
                                                                                {
                                                                                    DriveLetter = "C",
                                                                                    SizeInGb = 50,
                                                                                    Type = VolumeType.Standard,
                                                                                },
                                                                            new Volume
                                                                                {
                                                                                    DriveLetter = "D",
                                                                                    SizeInGb = 50,
                                                                                    Type = VolumeType.Standard,
                                                                                },
                                                                        },
            };

            return new ConsolidatedDeployment { Name = "DeploymentArcology", Packages = packages, DeploymentConfigurationOverride = deploymentConfigurationOverride };
        }

        /// <summary>
        /// Builds a mongo database server.
        /// </summary>
        /// <param name="name">Instance name.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="administratorPassword">Database administrator password.</param>
        /// <returns>Deployment to use.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Mongo wants lower case.")]
        public static ConsolidatedDeployment BuildTestMongoServerDeployment(string name, string databaseName, string administratorPassword)
        {
            new { name }.Must().NotBeNullNorWhiteSpace();
            new { databaseName }.Must().NotBeNullNorWhiteSpace();
            new { administratorPassword }.Must().NotBeNullNorWhiteSpace();

            var packages = new[]
                               {
                                   new PackageDescriptionWithOverrides
                                       {
                                           PackageDescription = new PackageDescription { Id = NullPackageId, },
                                           InitializationStrategies =
                                               new InitializationStrategyBase[]
                                                   {
                                                       new InitializationStrategyMongo
                                                           {
                                                               AdministratorPassword = administratorPassword,
                                                               DocumentDatabaseName = databaseName,
                                                               ManagementChannelName = databaseName.ToLowerInvariant(),
                                                           },
                                                   },
                                       },
                               };

            var deploymentConfigurationOverride = new DeploymentConfiguration
            {
                InstanceType =
                                                              new InstanceType
                                                              {
                                                                  SpecificInstanceTypeSystemId = "t2.small",
                                                                  OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.Base },
                                                              },
                Volumes = new[]
                                                                        {
                                                                            new Volume
                                                                                {
                                                                                    DriveLetter = "C",
                                                                                    SizeInGb = 50,
                                                                                    Type = VolumeType.Standard,
                                                                                },
                                                                            new Volume
                                                                                {
                                                                                    DriveLetter = "D",
                                                                                    SizeInGb = 50,
                                                                                    Type = VolumeType.Standard,
                                                                                },
                                                                        },
            };

            return new ConsolidatedDeployment { Name = name, Packages = packages, DeploymentConfigurationOverride = deploymentConfigurationOverride };
        }

        /// <summary>
        /// Converts the source secure string into a standard insecure string.
        /// </summary>
        /// <param name="source">The source secure string.</param>
        /// <returns>The standard insecure string.</returns>
        public static string ToInsecureString(this SecureString source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(source);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}