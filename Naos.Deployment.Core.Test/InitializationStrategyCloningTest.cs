// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyCloningTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System.Linq;

    using Naos.Cron;
    using Naos.Deployment.Contract;
    using Naos.MessageBus.DataContract;

    using Xunit;

    public class InitializationStrategyCloningTest
    {
        [Fact]
        public static void Clone_CertificateToInstall_Works()
        {
            var original = new InitializationStrategyCertificateToInstall { CertificateToInstall = "cert", UserToGrantPrivateKeyAccess = "someone" };
            var cloned = original.Clone() as InitializationStrategyCertificateToInstall;
            Assert.NotNull(cloned);
            Assert.Equal(original.CertificateToInstall, cloned.CertificateToInstall);
            Assert.Equal(original.UserToGrantPrivateKeyAccess, cloned.UserToGrantPrivateKeyAccess);
        }

        [Fact]
        public static void Clone_DirectoryToCreate_Works()
        {
            var original = new InitializationStrategyDirectoryToCreate
                               {
                                   DirectoryToCreate =
                                       new DirectoryToCreateDetails
                                           {
                                               FullControlAccount
                                                   =
                                                   "account",
                                               FullPath
                                                   =
                                                   @"D:\Dir"
                                           }
                               };
            var cloned = original.Clone() as InitializationStrategyDirectoryToCreate;
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            Assert.NotSame(original.DirectoryToCreate, cloned.DirectoryToCreate);
            Assert.Equal(original.DirectoryToCreate.FullControlAccount, cloned.DirectoryToCreate.FullControlAccount);
            Assert.Equal(original.DirectoryToCreate.FullPath, cloned.DirectoryToCreate.FullPath);
        }

        [Fact]
        public static void Clone_Iis_Works()
        {
            var original = new InitializationStrategyIis
                               {
                                   AppPoolStartMode = ApplicationPoolStartMode.AlwaysRunning,
                                   AutoStartProvider =
                                       new AutoStartProvider
                                           {
                                               Name = "Provider",
                                               Type = "Type"
                                           },
                                   PrimaryDns = "myDns",
                                   SslCertificateName = "certName",
                                   EnableHttp = true
                               };
            var cloned = original.Clone() as InitializationStrategyIis;
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            Assert.Equal(original.AppPoolStartMode, cloned.AppPoolStartMode);
            Assert.NotSame(original.AutoStartProvider, cloned.AutoStartProvider);
            Assert.Equal(original.AutoStartProvider.Name, cloned.AutoStartProvider.Name);
            Assert.Equal(original.AutoStartProvider.Type, cloned.AutoStartProvider.Type);
            Assert.Equal(original.PrimaryDns, cloned.PrimaryDns);
            Assert.Equal(original.SslCertificateName, cloned.SslCertificateName);
            Assert.Equal(original.EnableHttp, cloned.EnableHttp);
        }

        [Fact]
        public static void Clone_MessageBusHandler_Works()
        {
            var original = new InitializationStrategyMessageBusHandler
                               {
                                   ChannelsToMonitor =
                                       new[]
                                           { new Channel { Name = "channel" } },
                                   WorkerCount = 4
                               };
            var cloned = original.Clone() as InitializationStrategyMessageBusHandler;
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            Assert.Equal(original.WorkerCount, cloned.WorkerCount);
            Assert.Equal(original.ChannelsToMonitor.Count, cloned.ChannelsToMonitor.Count);
            Assert.NotSame(original.ChannelsToMonitor.Single(), cloned.ChannelsToMonitor.Single());
            Assert.Equal(original.ChannelsToMonitor.Single().Name, cloned.ChannelsToMonitor.Single().Name);
        }

        [Fact]
        public static void Clone_Mongo_Works()
        {
            var original = new InitializationStrategyMongo
                               {
                                   AdministratorPassword = "password",
                                   DocumentDatabaseName = "name",
                                   DataDirectory = @"D:\Data",
                                   LogDirectory = @"D:\Log"
                               };
            var cloned = original.Clone() as InitializationStrategyMongo;
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            Assert.Equal(original.AdministratorPassword, cloned.AdministratorPassword);
            Assert.Equal(original.DocumentDatabaseName, cloned.DocumentDatabaseName);
            Assert.Equal(original.DataDirectory, cloned.DataDirectory);
            Assert.Equal(original.LogDirectory, cloned.LogDirectory);
        }

        [Fact]
        public static void Clone_PrivateDnsEntry_Works()
        {
            var original = new InitializationStrategyDnsEntry { PrivateDnsEntry = "entry", PublicDnsEntry = "here" };
            var cloned = original.Clone() as InitializationStrategyDnsEntry;
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            Assert.Equal(original.PrivateDnsEntry, cloned.PrivateDnsEntry);
            Assert.Equal(original.PublicDnsEntry, cloned.PublicDnsEntry);
        }

        [Fact]
        public static void Clone_ScheduledTask_Works()
        {
            var original = new InitializationStrategyScheduledTask
                               {
                                   Name = "Name",
                                   Description = "Description",
                                   ExeName = "My Exe",
                                   Arguments = "Args",
                                   Schedule = new MinutelySchedule()
                               };

            var cloned = original.Clone() as InitializationStrategyScheduledTask;
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            Assert.Equal(original.Name, cloned.Name);
            Assert.Equal(original.Description, cloned.Description);
            Assert.Equal(original.ExeName, cloned.ExeName);
            Assert.Equal(original.Arguments, cloned.Arguments);
            Assert.Equal(ScheduleCronExpressionConverter.ToCronExpression(original.Schedule), ScheduleCronExpressionConverter.ToCronExpression(cloned.Schedule));
        }

        [Fact]
        public static void Clone_SqlServer_Works()
        {
            var original = new InitializationStrategySqlServer
                               {
                                   AdministratorPassword = "password",
                                   Name = "name",
                                   DataDirectory = @"D:\Data",
                                   BackupDirectory = @"D:\Backup",
                                   Restore = new NullDatabaseRestore(),
                                   Migration = new NullDatabaseMigration(),
                                   Create =
                                       new Create
                                           {
                                               DatabaseFileNameSettings =
                                                   new DatabaseFileNameSettings
                                                       {
                                                           LogFileLogicalName
                                                               =
                                                               "logname",
                                                           LogFileNameOnDisk
                                                               =
                                                               "log",
                                                           DataFileNameOnDisk
                                                               =
                                                               "data",
                                                           DataFileLogicalName
                                                               =
                                                               "dataname"
                                                       },
                                               DatabaseFileSizeSettings =
                                                   new DatabaseFileSizeSettings
                                                       {
                                                           DataFileMaxSizeInKb
                                                               =
                                                               1,
                                                           LogFileMaxSizeInKb
                                                               =
                                                               2,
                                                           DataFileGrowthSizeInKb
                                                               =
                                                               3,
                                                           LogFileCurrentSizeInKb
                                                               =
                                                               4,
                                                           LogFileGrowthSizeInKb
                                                               =
                                                               5,
                                                           DataFileCurrentSizeInKb
                                                               =
                                                               6
                                                       }
                                           }
                               };
            var cloned = original.Clone() as InitializationStrategySqlServer;
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned);
            Assert.Equal(original.AdministratorPassword, cloned.AdministratorPassword);
            Assert.Equal(original.BackupDirectory, cloned.BackupDirectory);
            Assert.Equal(original.DataDirectory, cloned.DataDirectory);
            Assert.Equal(original.Name, cloned.Name);
            Assert.NotSame(original.Migration, cloned.Migration);
            Assert.NotSame(original.Restore, cloned.Restore);
            Assert.NotSame(original.Create, cloned.Create);
            Assert.NotSame(original.Create.DatabaseFileNameSettings, cloned.Create.DatabaseFileNameSettings);
            Assert.NotSame(original.Create.DatabaseFileSizeSettings, cloned.Create.DatabaseFileSizeSettings);
            Assert.Equal(original.Create.DatabaseFileNameSettings.DataFileLogicalName, cloned.Create.DatabaseFileNameSettings.DataFileLogicalName);
            Assert.Equal(original.Create.DatabaseFileNameSettings.DataFileNameOnDisk, cloned.Create.DatabaseFileNameSettings.DataFileNameOnDisk);
            Assert.Equal(original.Create.DatabaseFileNameSettings.LogFileLogicalName, cloned.Create.DatabaseFileNameSettings.LogFileLogicalName);
            Assert.Equal(original.Create.DatabaseFileNameSettings.LogFileNameOnDisk, cloned.Create.DatabaseFileNameSettings.LogFileNameOnDisk);
            Assert.Equal(original.Create.DatabaseFileSizeSettings.DataFileCurrentSizeInKb, cloned.Create.DatabaseFileSizeSettings.DataFileCurrentSizeInKb);
            Assert.Equal(original.Create.DatabaseFileSizeSettings.DataFileGrowthSizeInKb, cloned.Create.DatabaseFileSizeSettings.DataFileGrowthSizeInKb);
            Assert.Equal(original.Create.DatabaseFileSizeSettings.DataFileMaxSizeInKb, cloned.Create.DatabaseFileSizeSettings.DataFileMaxSizeInKb);
            Assert.Equal(original.Create.DatabaseFileSizeSettings.LogFileCurrentSizeInKb, cloned.Create.DatabaseFileSizeSettings.LogFileCurrentSizeInKb);
            Assert.Equal(original.Create.DatabaseFileSizeSettings.LogFileGrowthSizeInKb, cloned.Create.DatabaseFileSizeSettings.LogFileGrowthSizeInKb);
            Assert.Equal(original.Create.DatabaseFileSizeSettings.LogFileMaxSizeInKb, cloned.Create.DatabaseFileSizeSettings.LogFileMaxSizeInKb);
        }
    }
}
