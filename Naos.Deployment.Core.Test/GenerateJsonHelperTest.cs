// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GenerateJsonHelperTest.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;

    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Handler;
    using Naos.Deployment.MessageBus.Scheduler;
    using Naos.Deployment.Persistence;
    using Naos.Deployment.Tracking;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;
    using Naos.Recipes.Configuration.Setup;
    using Naos.Serialization.Factory;

    using OBeautifulCode.TypeRepresentation;

    using Spritely.ReadModel.Mongo;
    using Spritely.Recipes;

    using Xunit;

    using MessageBusCredentials = Naos.MessageBus.Domain.Credentials;
    using SpritelyCredentials = Spritely.ReadModel.Credentials;

    public static class GenerateJsonHelperTest
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
            var customEventLog = "Application"; // can be something sepcific like your company name.

            // ----------- CHANGE ABOVE THIS LINE TO YOUR STUFF --------------
            var packages = new List<PackageDescriptionWithOverrides>();

            var deploymentHandlerPackageId = "Naos.Deployment.MessageBus.Hangfire.Console";
            var channelNameOne = "aws";
            var channelNameTwo = "cloud";
            var logFilePath = @"D:\Deployments\Naos.Deployment\packagedWebsite\DeploymentHandler.txt";
            var eventLogSource = "DeploymentHandler";
            var logConfigurations = new LogConfigurationBase[]
                                        {
                                            new LogConfigurationEventLog(LogContexts.AllErrors, eventLogSource, customEventLog),
                                            new LogConfigurationFile(LogContexts.All, logFilePath),
                                        };

            var configFileSerializationDescription = Config.ConfigFileSerializationDescription;

            var serializer = SerializerFactory.Instance.BuildSerializer(configFileSerializationDescription);
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
                            Password = deploymentDatabasePassword.ToSecureString(),
                        },
                    },
                },
            };

            var deploymentMessageHandlerSettings = new DeploymentMessageHandlerSettings
            {
                AccessKey = startStopAccessKey,
                SecretKey = startStopSecretKey,
                Environment = environment,
                SystemLocation = region,
                ContainerSystemLocation = availabilityZone,
                InfrastructureTrackerConfiguration = infrastructureTrackerConfig,
                InstanceNameLookupSource = InstanceNameLookupSource.Arcology,
            };

            var deploymentHandler = new PackageDescriptionWithOverrides
                                        {
                                            Id = deploymentHandlerPackageId,
                                            InitializationStrategies =
                                                new InitializationStrategyBase[]
                                                    {
                                                        new InitializationStrategyMessageBusHandler
                                                            {
                                                                ChannelsToMonitor = new[] { new SimpleChannel(channelNameOne), new SimpleChannel(channelNameTwo), },
                                                            },
                                                        new InitializationStrategyCreateEventLog { Source = eventLogSource, LogName = customEventLog },
                                                    },
                                            ItsConfigOverrides =
                                                new[]
                                                    {
                                                        new ItsConfigOverride
                                                            {
                                                                FileNameWithoutExtension = nameof(LogProcessorSettings),
                                                                FileContentsJson = serializer.SerializeToString(new LogProcessorSettings(logConfigurations)),
                                                            },
                                                        new ItsConfigOverride
                                                            {
                                                                FileNameWithoutExtension = "DeploymentMessageHandlerSettings",
                                                                FileContentsJson = serializer.SerializeToString(deploymentMessageHandlerSettings),
                                                            },
                                                    },
                                        };

            packages.Add(deploymentHandler);
            var output = serializer.SerializeToString(packages);
            Assert.NotNull(output);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Hangfire", Justification = "Spelling/name is correct.")]
        public static void CreateHangfireServerDeploymentJson()
        {
            var hangfireDns = "hangfire.development.com";
            var sslCertName = "SslCertName";
            var databaseServerUser = "sa";
            var databaseServerPassword = "thisPasswordShouldBeGood...";
            var customEventLog = "Application"; // can be something sepcific like your company name.

            // ----------- CHANGE ABOVE THIS LINE TO YOUR STUFF --------------
            var hangfireDatabasePackageId = "Naos.MessageBus.Hangfire.Database";
            var hangfireHarnessPackageId = "Naos.MessageBus.Hangfire.Harness";
            var logFilePath = @"D:\Deployments\Naos.MessageBus.Hangfire.Harness\packagedWebsite\HangfireHarnessLog.txt";
            var eventLogSource = "HangfireHarness";
            var logConfigurations = new LogConfigurationBase[]
                                        {
                                            new LogConfigurationEventLog(LogContexts.AllErrors, eventLogSource, customEventLog),
                                            new LogConfigurationFile(LogContexts.All, logFilePath),
                                        };

            var launchConfiguration = new LaunchConfiguration(
                TimeSpan.Zero,
                TypeMatchStrategy.NamespaceAndName,
                TypeMatchStrategy.NamespaceAndName,
                0,
                TimeSpan.FromMinutes(1),
                1,
                new[] { new SimpleChannel("default"), new SimpleChannel("hangsrvr"), });

            var databaseServer = "localhost";
            var configFileSerializationDescription = Config.ConfigFileSerializationDescription;

            var serializer = SerializerFactory.Instance.BuildSerializer(configFileSerializationDescription);

            var persistenceConnectionConfiguration = new MessageBusConnectionConfiguration
                                                         {
                                                             CourierPersistenceConnectionConfiguration =
                                                                 new CourierPersistenceConnectionConfiguration
                                                                     {
                                                                         Port = 1433,
                                                                         Server = databaseServer,
                                                                         Database = "Hangfire",
                                                                         Credentials =
                                                                             new MessageBusCredentials
                                                                                 {
                                                                                     User = databaseServerUser,
                                                                                     Password = databaseServerPassword.ToSecureString(),
                                                                                 },
                                                                     },
                                                             EventPersistenceConnectionConfiguration =
                                                                 new EventPersistenceConnectionConfiguration
                                                                     {
                                                                         Port = 1433,
                                                                         Server = databaseServer,
                                                                         Database = "ParcelTrackingEvents",
                                                                         Credentials =
                                                                             new MessageBusCredentials
                                                                                 {
                                                                                     User = databaseServerUser,
                                                                                     Password = databaseServerPassword.ToSecureString(),
                                                                                 },
                                                                     },
                                                             ReadModelPersistenceConnectionConfiguration =
                                                                 new ReadModelPersistenceConnectionConfiguration
                                                                     {
                                                                         Port = 1433,
                                                                         Server = databaseServer,
                                                                         Database = "ParcelTrackingReadModel",
                                                                         Credentials =
                                                                             new MessageBusCredentials
                                                                                 {
                                                                                     User = databaseServerUser,
                                                                                     Password = databaseServerPassword.ToSecureString(),
                                                                                 },
                                                                     },
                                                         };

            var hangfireDb = new PackageDescriptionWithOverrides
                                 {
                                     Id = hangfireDatabasePackageId,
                                     InitializationStrategies =
                                         new InitializationStrategyBase[]
                                                 { new InitializationStrategySqlServer { Name = "Hangfire", AdministratorPassword = databaseServerPassword, MessageBusBackChannelName = "Hangfire" }, },
                                 };

            var packages = new List<PackageDescriptionWithOverrides> { hangfireDb };

            var hangfireHarness = new PackageDescriptionWithOverrides
                                      {
                                          Id = hangfireHarnessPackageId,
                                          InitializationStrategies =
                                              new InitializationStrategyBase[]
                                                  {
                                                      new InitializationStrategyDnsEntry { PrivateDnsEntry = hangfireDns },
                                                      new InitializationStrategyCreateEventLog { Source = eventLogSource, LogName = customEventLog },
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
                                                                              "Naos.MessageBus.Hangfire.Harness.ApplicationPreload, Naos.MessageBus.Hangfire.Harness",
                                                                      },
                                                          },
                                                  },
                                          ItsConfigOverrides =
                                              new[]
                                                  {
                                                      new ItsConfigOverride
                                                          {
                                                              FileNameWithoutExtension = nameof(HandlerFactoryConfiguration),
                                                              FileContentsJson = serializer.SerializeToString(new HandlerFactoryConfiguration(TypeMatchStrategy.NamespaceAndName)),
                                                          },
                                                      new ItsConfigOverride
                                                          {
                                                              FileNameWithoutExtension = nameof(LogProcessorSettings),
                                                              FileContentsJson = serializer.SerializeToString(new LogProcessorSettings(logConfigurations)),
                                                          },
                                                      new ItsConfigOverride
                                                          {
                                                              FileNameWithoutExtension = nameof(MessageBusConnectionConfiguration),
                                                              FileContentsJson = serializer.SerializeToString(persistenceConnectionConfiguration),
                                                          },
                                                      new ItsConfigOverride
                                                          {
                                                              FileNameWithoutExtension = nameof(LaunchConfiguration),
                                                              FileContentsJson = serializer.SerializeToString(launchConfiguration),
                                                          },
                                                  },
                                      };

            packages.Add(hangfireHarness);
            var output = serializer.SerializeToString(packages);
            Assert.NotNull(output);
        }
    }
}
