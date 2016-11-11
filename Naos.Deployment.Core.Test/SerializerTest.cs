// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializerTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.Domain;

    using Xunit;

    public class SerializerTest
    {
        [Fact]
        public static void Deserialize_PrivateDnsEntry_Valid()
        {
            var input = @"
[{
	""id"": ""Naos.Something"",
	""initializationStrategies"": [{
		""privateDnsEntry"": ""something.database.development.cometrics.com""
	}]
}]";
            var deserialized = input.FromJson<ICollection<PackageDescriptionWithOverrides>>();

            Assert.NotNull(deserialized);
            var actualDns =
                deserialized.Single()
                    .InitializationStrategies.OfType<InitializationStrategyDnsEntry>()
                    .Single()
                    .PrivateDnsEntry;
            Assert.Equal("something.database.development.cometrics.com", actualDns);
        }

        [Fact]
        public static void Deserialize_ScheduledTask_Valid()
        {
            var input = @"
[{
	""id"": ""Naos.Something"",
	""initializationStrategies"": [{  ""name"": ""TheName"",  ""description"": ""Description To Have."", ""exeName"":""MyConsole.exe"", ""schedule"":{""cronExpression"":""* * * * *""}, ""arguments"":""/args""}]
}]";
            var deserialized = input.FromJson<ICollection<PackageDescriptionWithOverrides>>();

            Assert.NotNull(deserialized);
            var actualStrategy = deserialized.Single()
                .InitializationStrategies.OfType<InitializationStrategyScheduledTask>()
                .Single();
            Assert.Equal("TheName", actualStrategy.Name);
            Assert.Equal("Description To Have.", actualStrategy.Description);
            Assert.Equal("MyConsole.exe", actualStrategy.ExeName);
            Assert.Equal("/args", actualStrategy.Arguments);
            Assert.Equal("* * * * *", actualStrategy.Schedule.ToCronExpression());
        }

        [Fact]
        public static void Deserialize_DirectoryToCreate_Valid()
        {
            var input = @"
[{
	""id"": ""Naos.Something"",
	""initializationStrategies"": [{
		""directoryToCreate"": {""fullPath"": ""C:\\MyPath\\Is\\Here"", ""FullControlAccount"": ""Administrator"" }
	}]
}]";
            var deserialized = input.FromJson<ICollection<PackageDescriptionWithOverrides>>();

            Assert.NotNull(deserialized);
            var actualPath =
                deserialized.Single()
                    .InitializationStrategies.OfType<InitializationStrategyDirectoryToCreate>()
                    .Single()
                    .DirectoryToCreate.FullPath;
            Assert.Equal("C:\\MyPath\\Is\\Here", actualPath);
            var actualAccount = deserialized.Single()
                .InitializationStrategies.OfType<InitializationStrategyDirectoryToCreate>()
                .Single()
                .DirectoryToCreate.FullControlAccount;
            Assert.Equal("Administrator", actualAccount);
        }

        [Fact]
        public static void Deserialize_CertificateToInstall_Valid()
        {
            var input = @"
[{
	""id"": ""Naos.Something"",
	""initializationStrategies"": [{
		""certificateToInstall"": ""ThisIsTheNameOfTheCertInCertRetriever...""
	}]
}]";
            var deserialized = input.FromJson<ICollection<PackageDescriptionWithOverrides>>();

            Assert.NotNull(deserialized);
            var actualCert = deserialized.Single()
                .InitializationStrategies.OfType<InitializationStrategyCertificateToInstall>()
                .Single()
                .CertificateToInstall;
            Assert.Equal("ThisIsTheNameOfTheCertInCertRetriever...", actualCert);
        }

        [Fact]
        public static void Deserialize_DatabaseRestore_Valid()
        {
            var input = @"
[{ 
""id"": ""Naos.Something"", 
""initializationStrategies"": 
    [{""name"": ""DatabaseName"",      ""restore"": {""runChecksum"":true    }}]
}]";
            var deserialized = input.FromJson<ICollection<PackageDescriptionWithOverrides>>();

            Assert.NotNull(deserialized);
            var actualRestore = deserialized.Single()
                .InitializationStrategies.OfType<InitializationStrategySqlServer>()
                .Single()
                .Restore;
            var actualS3Restore = Assert.IsType<DatabaseRestoreFromS3>(actualRestore);
            Assert.NotNull(actualS3Restore);
            Assert.True(actualS3Restore.RunChecksum);
        }

        [Fact]
        public static void Deserialize_DatabaseMigration_Valid()
        {
            var input = @"
[{ 
""id"": ""Naos.Something"", 
""initializationStrategies"": 
    [{""name"": ""DatabaseName"", ""administratorPassword"":""hello"",      ""migration"": {""version"":17}}]
}]";
            var deserialized = input.FromJson<ICollection<PackageDescriptionWithOverrides>>();

            Assert.NotNull(deserialized);
            var actualMigration = deserialized.Single()
                .InitializationStrategies.OfType<InitializationStrategySqlServer>()
                .Single()
                .Migration;
            Assert.NotNull(actualMigration);
            var actualFluentMigration = Assert.IsType<DatabaseMigrationFluentMigrator>(actualMigration);
            Assert.NotNull(actualFluentMigration);
            Assert.Equal(17, actualFluentMigration.Version);
        }

        [Fact]
        public static void Deserialize_SingleMessageBusHandlerInitStrategy_Valid()
        {
            var input = @"
{
	""instanceType"": { ""VirtualCores"": 2, ""RamInGb"": 3, },
	""isPubliclyAccessible"": false,
	""volumes"": [{
		""driveLetter"": ""C"",
		""sizeInGb"": ""50"",
	}],
	""initializationStrategies"": [{
		""channelsToMonitor"": [{""name"":""MyChannel""}],
	}],
}
";

            var deserialized = input.FromJson<DeploymentConfigurationWithStrategies>();

            Assert.Equal(typeof(InitializationStrategyMessageBusHandler), deserialized.InitializationStrategies.Single().GetType());
            Assert.Equal("MyChannel", deserialized.InitializationStrategies.Cast<InitializationStrategyMessageBusHandler>().Single().ChannelsToMonitor.OfType<SimpleChannel>().Single().Name);
        }

        [Fact]
        public static void Deserialize_SingleDatabaseInitStrategy_Valid()
        {
            var input = @"
{
	""instanceType"": { ""VirtualCores"": 2, ""RamInGb"": 3, },
	""isPubliclyAccessible"": false,
	""volumes"": [{
		""driveLetter"": ""C"",
		""sizeInGb"": ""50"",
	}],
	""initializationStrategies"": [{
		""name"": ""Monkey"",
		""administratorPassword"": ""MyPassWord1234"",
	}],
}
";

            var deserialized = input.FromJson<DeploymentConfigurationWithStrategies>();

            Assert.Equal(typeof(InitializationStrategySqlServer), deserialized.InitializationStrategies.Single().GetType());
            Assert.Equal("Monkey", deserialized.InitializationStrategies.Cast<InitializationStrategySqlServer>().Single().Name);
        }

        [Fact]
        public static void Deserialize_SingleWebInitStrategy_Valid()
        {
            var input = @"
{
	""instanceType"": { ""virtualCores"": 2, ""ramInGb"": 3, },
	""isPubliclyAccessible"": false,
	""volumes"": [{
		""driveLetter"": ""C"",
		""sizeInGb"": ""50"",
	}],
	""InitializationStrategies"": [{
		""primaryDns"": ""reports.coopmetrics.coop"",
	}],
}
";

            var deserialized = input.FromJson<DeploymentConfigurationWithStrategies>();

            Assert.Equal(typeof(InitializationStrategyIis), deserialized.InitializationStrategies.Single().GetType());
            Assert.Equal("reports.coopmetrics.coop", deserialized.InitializationStrategies.Cast<InitializationStrategyIis>().Single().PrimaryDns);
        }

        [Fact]
        public static void Deserialize_SingleSelfHostInitStrategy_Valid()
        {
            var input = @"
{
	""instanceType"": { ""virtualCores"": 2, ""ramInGb"": 3, },
	""isPubliclyAccessible"": false,
	""volumes"": [{
		""driveLetter"": ""C"",
		""sizeInGb"": ""50"",
	}],
	""InitializationStrategies"": [{
		""selfHostSupportedDnsEntries"": [ ""reports.something.com""],
		""sslCertificateName"": ""MyCert"",
		""selfHostExeName"": ""My.exe"",
		""scheduledTaskAccount"": ""Monkey"",
	}],
}
";

            var deserialized = input.FromJson<DeploymentConfigurationWithStrategies>();

            Assert.Equal(typeof(InitializationStrategySelfHost), deserialized.InitializationStrategies.Single().GetType());
            Assert.Equal("reports.something.com", deserialized.InitializationStrategies.Cast<InitializationStrategySelfHost>().Single().SelfHostSupportedDnsEntries.Single());
            Assert.Equal("My.exe", deserialized.InitializationStrategies.Cast<InitializationStrategySelfHost>().Single().SelfHostExeName);
            Assert.Equal("MyCert", deserialized.InitializationStrategies.Cast<InitializationStrategySelfHost>().Single().SslCertificateName);
            Assert.Equal("Monkey", deserialized.InitializationStrategies.Cast<InitializationStrategySelfHost>().Single().ScheduledTaskAccount);
        }
    }
}
