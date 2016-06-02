// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GenerateJsonHelperTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;

    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Contract;
    using Naos.Deployment.MessageBus.Handler;
    using Naos.Deployment.Persistence;
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.Domain;

    using Spritely.ReadModel.Mongo;
    using Spritely.Recipes;

    using Xunit;

    using MessageBusCredentials = Naos.MessageBus.Domain.Credentials;
    using Serializer = Naos.Deployment.Domain.Serializer;
    using SpritelyCredentials = Spritely.ReadModel.Credentials;

    public class GenerateJsonHelperTest
    {
        [Fact]
        public static void CreateDeploymentHandlerDeploymentJson()
        {
            var deploymentDatabaseDns = "Deployment.database.development.com";
            var deploymentDatabaseUser = "user";
            var deploymentDatabasePassword = "aPasswordThatIsGood";

            var region = "sa-east-1";
            var availabilityZone = "sa-east-1a";
            var startStopAccessKey = "KAIIIDIIIDIDID";
            var startStopSecretKey = "a;sldkjfalksjdfklasjdflkasdfjkjsdfs";
            var environment = "Development";

            // ----------- CHANGE ABOVE THIS LINE TO YOUR STUFF --------------
            var packages = new List<PackageDescriptionWithOverrides>();

            var deploymentHandlerPackageId = "Naos.Deployment.MessageBus.Handler";
            var channelNameOne = "aws";
            var channelNameTwo = "cloud";
            var infrastructureTrackerConfig = new InfrastructureTrackerConfigurationDatabase
            {
                Database = new DeploymentDatabase
                {
                    ConnectionSettings = new MongoConnectionSettings
                    {
                        Database = environment,
                        Port = 27017,
                        Server = deploymentDatabaseDns,
                        Credentials = new SpritelyCredentials
                        {
                            User = deploymentDatabaseUser,
                            Password = deploymentDatabasePassword.ToSecureString()
                        }
                    }
                }
            };

            var deploymentMessageHandlerSettings = new DeploymentMessageHandlerSettings
            {
                AccessKey = startStopAccessKey,
                SecretKey = startStopSecretKey,
                Environment = environment,
                SystemLocation = region,
                ContainerSystemLocation = availabilityZone,
                InfrastructureTrackerConfiguration = infrastructureTrackerConfig,
                InstanceNameLookupSource = InstanceNameLookupSource.Arcology
            };

            var deploymentHandler = new PackageDescriptionWithOverrides
                                        {
                                            Id = deploymentHandlerPackageId,
                                            InitializationStrategies =
                                                new InitializationStrategyBase[]
                                                    {
                                                        new InitializationStrategyMessageBusHandler
                                                            {
                                                                ChannelsToMonitor =
                                                                    new[]
                                                                        {
                                                                            new SimpleChannel(
                                                                                channelNameOne),
                                                                            new SimpleChannel(
                                                                                channelNameTwo)
                                                                        }
                                                            }
                                                    },
                                            ItsConfigOverrides =
                                                new[]
                                                    {
                                                        new ItsConfigOverride
                                                            {
                                                                FileNameWithoutExtension =
                                                                    "DeploymentMessageHandlerSettings",
                                                                FileContentsJson =
                                                                    Serializer.Serialize(
                                                                        deploymentMessageHandlerSettings,
                                                                        true)
                                                            }
                                                    }
                                        };

            packages.Add(deploymentHandler);
            var output = Serializer.Serialize(packages, true);
            Assert.NotNull(output);
        }

        [Fact]
        public static void CreateHangfireServerDeploymentJson()
        {
            var hangfireDns = "hangfire.development.com";
            var sslCertName = "RootSslCertName";
            var databaseServerPassword = "thisPasswordShouldBeGood...";

            // ----------- CHANGE ABOVE THIS LINE TO YOUR STUFF --------------
            var hangfireDatabasePackageId = "Naos.MessageBus.Hangfire.Database";
            var hangfireHarnessPackageId = "Naos.MessageBus.Hangfire.Harness";
            var logFilePath = @"D:\Deployments\Naos.MessageBus.Hangfire.Harness\packagedWebsite\HangfireHarnessLog.txt";
            var databaseServerUser = "sa";
            var databaseServer = "localhost";
            var persistenceConnectionConfiguration = new MessageBusConnectionConfiguration
                                                         {
                                                             CourierPersistenceConnectionConfiguration =
                                                                 new CourierPersistenceConnectionConfiguration
                                                                     {
                                                                         Port = 1433,
                                                                         Server = databaseServer,
                                                                         Database = "Hangfire",
                                                                         Credentials = new MessageBusCredentials
                                                                         {
                                                                             User = databaseServerUser,
                                                                             Password = databaseServerPassword.ToSecureString()
                                                                         }
                                                                 },
                                                             EventPersistenceConnectionConfiguration =
                                                                 new EventPersistenceConnectionConfiguration
                                                                     {
                                                                         Port = 1433,
                                                                         Server = databaseServer,
                                                                         Database = "ParcelTrackingEvents",
                                                                         Credentials = new MessageBusCredentials
                                                                         {
                                                                                     User = databaseServerUser,
                                                                                     Password = databaseServerPassword.ToSecureString()
                                                                                 }
                                                                     },
                                                             ReadModelPersistenceConnectionConfiguration =
                                                                 new ReadModelPersistenceConnectionConfiguration
                                                                     {
                                                                         Port = 1433,
                                                                         Server = databaseServer,
                                                                         Database = "ParcelTrackingReadModel",
                                                                         Credentials = new MessageBusCredentials
                                                                         {
                                                                             User = databaseServerUser,
                                                                             Password = databaseServerPassword.ToSecureString()
                                                                         }
                                                                 }
            };

            var hangfireDb = new PackageDescriptionWithOverrides
                                 {
                                     Id = hangfireDatabasePackageId,
                                     InitializationStrategies =
                                         new InitializationStrategyBase[]
                                             {
                                                 new InitializationStrategySqlServer
                                                     {
                                                         Name = "Hangfire",
                                                         AdministratorPassword = databaseServerPassword
                                                     }
                                             }
                                 };

            var packages = new List<PackageDescriptionWithOverrides> { hangfireDb };

            var hangfireHarness = new PackageDescriptionWithOverrides
                                      {
                                          Id = hangfireHarnessPackageId,
                                          InitializationStrategies =
                                              new InitializationStrategyBase[]
                                                  {
                                                      new InitializationStrategyDnsEntry { PrivateDnsEntry = hangfireDns },
                                                      new InitializationStrategyIis
                                                          {
                                                              AppPoolStartMode =
                                                                  ApplicationPoolStartMode.AlwaysRunning,
                                                              PrimaryDns = hangfireDns,
                                                              SslCertificateName = sslCertName,
                                                              AutoStartProvider =
                                                                  new AutoStartProvider
                                                                      {
                                                                          Name =
                                                                              "ApplicationPreload",
                                                                          Type =
                                                                              "Naos.MessageBus.Hangfire.Harness.ApplicationPreload, Naos.MessageBus.Hangfire.Harness"
                                                                      }
                                                          }
                                                  },
                                          ItsConfigOverrides =
                                              new[]
                                                  {
                                                      new ItsConfigOverride
                                                          {
                                                              FileNameWithoutExtension =
                                                                  "MessageBusHarnessSettings",
                                                              FileContentsJson =
                                                                  Serializer.Serialize(
                                                                      new MessageBusHarnessSettings
                                                                          {
                                                                              LogProcessorSettings
                                                                                  =
                                                                                  new LogProcessorSettings
                                                                                      {
                                                                                          LogFilePath
                                                                                              =
                                                                                              logFilePath
                                                                                      },
                                                                              ConnectionConfiguration
                                                                                  =
                                                                                  persistenceConnectionConfiguration,
                                                                              RoleSettings
                                                                                  =
                                                                                  new MessageBusHarnessRoleSettingsBase[]
                                                                                      {
                                                                                          new MessageBusHarnessRoleSettingsHost
                                                                                              {
                                                                                                  RunDashboard
                                                                                                      =
                                                                                                      true
                                                                                              },
                                                                                          new MessageBusHarnessRoleSettingsExecutor
                                                                                              {
                                                                                                  TypeMatchStrategy
                                                                                                      =
                                                                                                      TypeMatchStrategy
                                                                                                      .NamespaceAndName,
                                                                                                  ChannelsToMonitor
                                                                                                      =
                                                                                                      new[]
                                                                                                          {
                                                                                                              new SimpleChannel("default"),
                                                                                                              new SimpleChannel("hangsrvr")
                                                                                                          },
                                                                                                  HandlerAssemblyPath
                                                                                                      =
                                                                                                      @"D:\Deployments",
                                                                                                  WorkerCount
                                                                                                      =
                                                                                                      1,
                                                                                                  PollingTimeSpan
                                                                                                      =
                                                                                                      TimeSpan.FromMinutes(1)
                                                                                              }
                                                                                      }
                                                                          },
                                                                      true)
                                                          }
                                                  }
                                      };

            packages.Add(hangfireHarness);
            var output = Serializer.Serialize(packages, true);
            Assert.NotNull(output);
        }
    }
}
