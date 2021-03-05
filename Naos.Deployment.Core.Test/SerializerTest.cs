// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializerTest.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using Naos.Configuration.Domain;
    using Naos.Cron;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.Logging.Domain;
    using Naos.Logging.Persistence;
    using Naos.MessageBus.Domain;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Bson;
    using OBeautifulCode.Serialization.Json;
    using Xunit;
    using static System.FormattableString;

    public static class SerializerTest
    {
        private static readonly ObcJsonSerializer JsonSerializerToUse = new ObcJsonSerializer(typeof(NaosDeploymentCoreJsonSerializationConfiguration).ToJsonSerializationConfigurationType());

        [Fact]
        public static void Serializing_Logging_Config___Roundtrips()
        {
            // Arrange
            var configFileManager = new ConfigFileManager(new[] { Config.CommonPrecedence }, Config.DefaultConfigDirectoryName, JsonSerializerToUse);

            var expected = new[]
                              {
                                  new TimeSlicedFilesLogConfig(
                                      new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                                      "{deploymentDriveLetter}:\\Logs",
                                      "All",
                                      TimeSpan.FromMinutes(10),
                                      true,
                                      LogItemPropertiesToIncludeInLogMessage.Default),
                                  new TimeSlicedFilesLogConfig(
                                      new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                                      "{deploymentDriveLetter}:\\SerializedLogs",
                                      "SerializedLog",
                                      TimeSpan.FromMinutes(10),
                                      true,
                                      LogItemPropertiesToIncludeInLogMessage.LogItemSerialization),
                                  new TimeSlicedFilesLogConfig(
                                      new Dictionary<LogItemKind, IReadOnlyCollection<string>>(),
                                      "{deploymentDriveLetter}:\\SerializedTelemetry",
                                      "SerializedTelemetry",
                                      TimeSpan.FromMinutes(10),
                                      true,
                                      LogItemPropertiesToIncludeInLogMessage.LogItemSerialization),
                              };

            // Act
            var actualJson = configFileManager.SerializeConfigToFileText(expected);
            var actualObject = configFileManager.DeserializeConfigFileText<IReadOnlyCollection<LogWriterConfigBase>>(actualJson);
            var roundJson = configFileManager.SerializeConfigToFileText(actualObject);

            // Assert
            actualJson.Should().NotBeNull();
            actualObject.Should().NotBeNull();
            roundJson.Should().Be(actualJson);
        }

        [Fact]
        public static void RoundtripSerializeDeserialize___Using_SerializerRepresentation___Works()
        {
            // Arrange
            var expected = new DeployedInstance
                               {
                                   InstanceDescription =
                                       new InstanceDescription
                                           {
                                               DeployedPackages =
                                                   new[]
                                                       {
                                                           new PackageDescriptionWithDeploymentStatus
                                                               {
                                                                   InitializationStrategies
                                                                       = new InitializationStrategyBase[]
                                                                             {
                                                                                 new
                                                                                     InitializationStrategyDnsEntry
                                                                                         {
                                                                                             PrivateDnsEntry = "something",
                                                                                         },
                                                                                 new
                                                                                     InitializationStrategyScheduledTask
                                                                                         {
                                                                                             Schedule = new DailyScheduleInUtc(),
                                                                                         },
                                                                             },
                                                               },
                                                       }.ToDictionary(k => Guid.NewGuid().ToString(), v => v),
                                           },
                               };

            void ThrowIfObjectsDiffer(object actualAsObject)
            {
                var actual = actualAsObject as DeployedInstance;
                actual.Should().NotBeNull();
                actual.InstanceDescription.DeployedPackages.First().Value.InitializationStrategies.First().GetType().Should().Be(expected.InstanceDescription.DeployedPackages.First().Value.InitializationStrategies.First().GetType());
                actual.InstanceDescription.DeployedPackages.First().Value.InitializationStrategies.Skip(1).First().GetType().Should().Be(expected.InstanceDescription.DeployedPackages.First().Value.InitializationStrategies.Skip(1).First().GetType());
            }

            // Act & Assert
            ActAndAssertForRoundtripSerialization(expected, ThrowIfObjectsDiffer, new ObcBsonSerializer<DeploymentBsonSerializationConfiguration>());
        }

        [Fact]
        public static void RoundtripSerializeDeserializeDeploymentConfig___Using_SerializerRepresentation___Works()
        {
            // Arrange
            var expected = new DeploymentConfiguration
                               {
                                   InstanceType = new InstanceType { OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.Base } },
                               };

            void ThrowIfObjectsDiffer(object actualAsObject)
            {
                var actual = actualAsObject as DeploymentConfiguration;
                actual.Should().NotBeNull();
                var windowsOs = actual.InstanceType.OperatingSystem as OperatingSystemDescriptionWindows;
                windowsOs.Should().NotBeNull();
                windowsOs.Should().Be(expected.InstanceType.OperatingSystem);
            }

            // Act & Assert
            ActAndAssertForRoundtripSerialization(expected, ThrowIfObjectsDiffer, new ObcBsonSerializer<DeploymentBsonSerializationConfiguration>());
        }

        private static void ActAndAssertForRoundtripSerialization(object expected, Action<object> throwIfObjectsDiffer, ObcBsonSerializer bsonSerializer, bool testBson = true, bool testJson = true)
        {
            var stringSerializers = new List<IStringSerializeAndDeserialize>();
            var binarySerializers = new List<IBinarySerializeAndDeserialize>();

            if (testJson)
            {
                stringSerializers.Add(JsonSerializerToUse);
                binarySerializers.Add(JsonSerializerToUse);
            }

            if (testBson)
            {
                stringSerializers.Add(bsonSerializer);
                binarySerializers.Add(bsonSerializer);
            }

            if (!stringSerializers.Any() || !binarySerializers.Any())
            {
                throw new InvalidOperationException("no serializers are being tested");
            }

            foreach (var stringSerializer in stringSerializers)
            {
                var actualString = stringSerializer.SerializeToString(expected);
                var actualObject = stringSerializer.Deserialize(actualString, expected.GetType());

                try
                {
                    throwIfObjectsDiffer(actualObject);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(Invariant($"Failure with {nameof(stringSerializer)} - {stringSerializer.GetType()}"), ex);
                }
            }

            foreach (var binarySerializer in binarySerializers)
            {
                var actualBytes = binarySerializer.SerializeToBytes(expected);
                var actualObject = binarySerializer.Deserialize(actualBytes, expected.GetType());

                try
                {
                    throwIfObjectsDiffer(actualObject);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(Invariant($"Failure with {nameof(binarySerializer)} - {binarySerializer.GetType()}"), ex);
                }
            }
        }

        [Fact]
        public static void Deserialize_PrivateDnsEntry_Valid()
        {
            var input = @"
[{
	""PackageDescription"" : { ""id"": ""Naos.Something"" },
""initializationStrategies"": [{
		""privateDnsEntry"": ""something.database.development.naosproject.com""
	}]
}]";
            var deserialized = (ICollection<PackageDescriptionWithOverrides>)JsonSerializerToUse.Deserialize(input, typeof(ICollection<PackageDescriptionWithOverrides>));

            Assert.NotNull(deserialized);
            var actualDns =
                deserialized.Single()
                    .InitializationStrategies.OfType<InitializationStrategyDnsEntry>()
                    .Single()
                    .PrivateDnsEntry;
            Assert.Equal("something.database.development.naosproject.com", actualDns);
        }

        [Fact]
        public static void Deserialize_ScheduledTask_Valid()
        {
            var input = @"
[{
	""PackageDescription"" : { ""id"": ""Naos.Something"" },
""initializationStrategies"": [{  ""name"": ""TheName"",  ""description"": ""Description To Have."", ""exeFilePathRelativeToPackageRoot"":""MyConsole.exe"", ""runElevated"":""true"", ""schedule"":{""cronExpression"":""* * * * *""}, ""arguments"":""/args"", ""scheduledTaskAccount"":""administrator""}]
}]";
            var deserialized = (ICollection<PackageDescriptionWithOverrides>)JsonSerializerToUse.Deserialize(input, typeof(ICollection<PackageDescriptionWithOverrides>));

            Assert.NotNull(deserialized);
            var actualStrategy = deserialized.Single()
                .InitializationStrategies.OfType<InitializationStrategyScheduledTask>()
                .Single();
            Assert.Equal("TheName", actualStrategy.Name);
            Assert.Equal("Description To Have.", actualStrategy.Description);
            Assert.Equal("MyConsole.exe", actualStrategy.ExeFilePathRelativeToPackageRoot);
            Assert.Equal("/args", actualStrategy.Arguments);
            Assert.Equal("* * * * *", actualStrategy.Schedule.ToCronExpression());
        }

        [Fact]
        public static void Deserialize_DirectoryToCreate_Valid()
        {
            var input = @"
[{
	""PackageDescription"" : { ""id"": ""Naos.Something"" },
""initializationStrategies"": [{
		""directoryToCreate"": {""fullPath"": ""C:\\MyPath\\Is\\Here"", ""FullControlAccount"": ""Administrator"" }
	}]
}]";
            var deserialized = (ICollection<PackageDescriptionWithOverrides>)JsonSerializerToUse.Deserialize(input, typeof(ICollection<PackageDescriptionWithOverrides>));

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
	""PackageDescription"" : { ""id"": ""Naos.Something"" },
	""initializationStrategies"": [{
		""certificateToInstall"": ""ThisIsTheNameOfTheCertInCertRetriever...""
	}]
}]";
            var deserialized = (ICollection<PackageDescriptionWithOverrides>)JsonSerializerToUse.Deserialize(input, typeof(ICollection<PackageDescriptionWithOverrides>));

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
	""PackageDescription"" : { ""id"": ""Naos.Something"" },
""initializationStrategies"":
    [{""name"": ""DatabaseName"",      ""restore"": {""runChecksum"":true    }}]
}]";
            var deserialized = (ICollection<PackageDescriptionWithOverrides>)JsonSerializerToUse.Deserialize(input, typeof(ICollection<PackageDescriptionWithOverrides>));

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

            var deserialized = (DeploymentConfigurationWithStrategies)JsonSerializerToUse.Deserialize(input, typeof(DeploymentConfigurationWithStrategies));

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

            var deserialized = (DeploymentConfigurationWithStrategies)JsonSerializerToUse.Deserialize(input, typeof(DeploymentConfigurationWithStrategies));

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
		""primaryDns"": ""reports.naosproject.com"",
	}],
}
";

            var deserialized = (DeploymentConfigurationWithStrategies)JsonSerializerToUse.Deserialize(input, typeof(DeploymentConfigurationWithStrategies));

            Assert.Equal(typeof(InitializationStrategyIis), deserialized.InitializationStrategies.Single().GetType());
            Assert.Equal("reports.naosproject.com", deserialized.InitializationStrategies.Cast<InitializationStrategyIis>().Single().PrimaryDns);
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
		""selfHostExeFilePathRelativeToPackageRoot"": ""My.exe"",
		""scheduledTaskAccount"": ""Monkey"",
	}],
}
";

            var deserialized = (DeploymentConfigurationWithStrategies)JsonSerializerToUse.Deserialize(input, typeof(DeploymentConfigurationWithStrategies));

            Assert.Equal(typeof(InitializationStrategySelfHost), deserialized.InitializationStrategies.Single().GetType());
            Assert.Equal("reports.something.com", deserialized.InitializationStrategies.Cast<InitializationStrategySelfHost>().Single().SelfHostSupportedDnsEntries.Single());
            Assert.Equal("My.exe", deserialized.InitializationStrategies.Cast<InitializationStrategySelfHost>().Single().SelfHostExeFilePathRelativeToPackageRoot);
            Assert.Equal("MyCert", deserialized.InitializationStrategies.Cast<InitializationStrategySelfHost>().Single().SslCertificateName);
            Assert.Equal("Monkey", deserialized.InitializationStrategies.Cast<InitializationStrategySelfHost>().Single().ScheduledTaskAccount);
        }
    }
}
