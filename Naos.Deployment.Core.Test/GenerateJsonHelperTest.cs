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
    using Naos.MessageBus.DataContract;
    using Naos.MessageBus.HandlingContract;

    using Spritely.ReadModel;
    using Spritely.ReadModel.Mongo;
    using Spritely.Recipes;

    using Xunit;

    public class GenerateJsonHelperTest
    {
        [Fact]
        public static void CreateHangfireServerDeploymentJson()
        {
            var packages = new List<PackageDescriptionWithOverrides>();

            var logFilePath = @"D:\Deployments\HangfireHarnessLog.txt";
            var hangfireDns = "hangfire.development.com";
            var sslCertName = "RootSslCertName";
            var hangfireDatabasePackageId = "Naos.MessageBus.Hangfire.Database";
            var hangfireHarnessPackageId = "Naos.MessageBus.Hangfire.Harness";
            var hangfirePassword = "thisPasswordShouldBeGood...";
            var persistenceConnectionString = "Server=localhost;Database=Hangfire;user id=sa;password=" + hangfirePassword + ";";

            var hangfireDb = new PackageDescriptionWithOverrides
                                 {
                                     Id = hangfireDatabasePackageId,
                                     InitializationStrategies =
                                         new InitializationStrategyBase[]
                                             {
                                                 new InitializationStrategySqlServer
                                                     {
                                                         Name = "Hangfire",
                                                         AdministratorPassword = hangfirePassword
                                                     }
                                             }
                                 };

            packages.Add(hangfireDb);

            var hangfireHarness = new PackageDescriptionWithOverrides
                               {
                                   Id = hangfireHarnessPackageId,
                                   InitializationStrategies =
                                       new InitializationStrategyBase[]
                                           {
                                               new InitializationStrategyDnsEntry
                                                   {
                                                       PrivateDnsEntry = hangfireDns
                                                   },
                                               new InitializationStrategyIis
                                                   {
                                                       AppPoolStartMode = ApplicationPoolStartMode.AlwaysRunning,
                                                       PrimaryDns = hangfireDns,
                                                       SslCertificateName = sslCertName,
                                                       AutoStartProvider = new AutoStartProvider
                                                                               {
                                                                                   Name = "ApplicationPreload",
                                                                                   Type = "Naos.MessageBus.Hangfire.Harness.ApplicationPreload, Naos.MessageBus.Hangfire.Harness"
                                                                               }
                                                   }
                                           },
                                   ItsConfigOverrides = new[]
                                                            {
                                                                new ItsConfigOverride
                                                                    {
                                                                        FileNameWithoutExtension = "MessageBusHarnessSettings",
                                                                        FileContentsJson = Serializer.Serialize(new MessageBusHarnessSettings
                                                                                                                    {
                                                                                                                        LogProcessorSettings = new LogProcessorSettings
                                                                                                                                                   {
                                                                                                                                                       LogFilePath = logFilePath
                                                                                                                                                   },
                                                                                                                        PersistenceConnectionString = persistenceConnectionString,
                                                                                                                        RoleSettings = new MessageBusHarnessRoleSettingsBase[]
                                                                                                                                           {
                                                                                                                                               new MessageBusHarnessRoleSettingsHost
                                                                                                                                                   {
                                                                                                                                                       RunDashboard = true
                                                                                                                                                   },
                                                                                                                                               new MessageBusHarnessRoleSettingsExecutor
                                                                                                                                                   {
                                                                                                                                                       TypeMatchStrategy = TypeMatchStrategy.NamespaceAndName,
                                                                                                                                                       ChannelsToMonitor = new[] { new Channel { Name = "default" }, new Channel { Name = "hangsrvr" } },
                                                                                                                                                       HandlerAssemblyPath = @"D:\Deployments",
                                                                                                                                                       WorkerCount = 1,
                                                                                                                                                       PollingTimeSpan = TimeSpan.FromMinutes(1)
                                                                                                                                                   }
                                                                                                                                           }
                                                                                                                    })
                                                                    }
                                                            }
                               };

            packages.Add(hangfireHarness);
            var output = Serializer.Serialize(packages, true);
            Assert.NotNull(output);
        }
    }
}
