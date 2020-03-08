// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DebugSeeder.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using MongoDB.Bson;
    using Naos.Deployment.Core.CertificateManagement;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.Deployment.Tracking;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Type;
    using Spritely.ReadModel.Mongo;
    using Spritely.Recipes;
    using Xunit;
    using Credentials = Spritely.ReadModel.Credentials;

    public static class DebugSeeder
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arcology", Justification = "Spelling/name is correct.")]
        [Fact(Skip = "Debug test designed to aid in setting up dependent items for deploying.")]
        public static void Debug_CreateArcologyInDatabase()
        {
            // environment the certificates are for
            var environment = "Development";

            // id for the database (usually just use the environment)
            var databaseId = environment;

            // database name to store records in
            var databaseName = "Deployment";

            // database user to connect to database to store in
            var databaseUser = "sa";

            // database user password to connect to database to store in
            var databasePassword = "password".ToSecureString();

            var arcologyInfo = new ArcologyInfo();
            var deployedInstances = new List<DeployedInstance>();

            var database = new DeploymentDatabase
            {
                ConnectionSettings = new MongoConnectionSettings
                {
                    Server = "deployment.database.url",
                    Port = 27017,
                    Database = databaseName,
                    Credentials = new Credentials
                    {
                        User = databaseUser,
                        Password = databasePassword,
                    },
                },
            };

            SerializationConfigurationManager.Configure<DeploymentBsonConfiguration>();

            var arcologyInfoCommands = database.GetCommandsInterface<string, ArcologyInfoContainer>();
            var arcologyInfoContainer = new ArcologyInfoContainer
                                            {
                                                Id = databaseId,
                                                Environment = environment,
                                                ArcologyInfo = arcologyInfo,
                                            };

            arcologyInfoCommands.AddOrUpdateOneAsync(arcologyInfoContainer).Wait();

            var instanceCommands = database.GetCommandsInterface<string, InstanceContainer>();
            var instanceContainers =
                deployedInstances.Select(MongoInfrastructureTracker.CreateInstanceContainerFromInstance)
                    .ToList();

            var instanceContainersToAddOrUpdate = instanceContainers.ToDictionary(key => key.Id, value => value);
            instanceCommands.AddOrUpdateManyAsync(instanceContainersToAddOrUpdate).Wait();
        }

        [Fact(Skip = "Debug test designed to aid in setting up dependent items for deploying.")]
        public static async Task Debug_CreateCertificateEntryInDatabase()
        {
            // this is because the passwords are encrypted, so this is just a convenience test for loading the db

            // environment the certificates are for
            var environment = "Development";

            // id for the database (usually just use the environment)
            var databaseId = environment;

            // server dns
            var server = "deployment.database.development.domain";

            // database name to store records in
            var databaseName = "Deployment";

            // database user to connect to database to store in
            var databaseUser = "sa";

            // database user password to connect to database to store in
            var databasePassword = "password".ToSecureString();

            var certificatesToLoad = new[]
                                         {
                                             new CertificateDescriptionWithClearPfxPayload(
                                                 "DevelopmentCertificate",
                                                 "ThumbprintOfCertificate",
                                                 new UtcDateTimeRangeInclusive(
                                                     new DateTime(2010, 10, 10, 0, 0, 0, 0, DateTimeKind.Utc),
                                                     new DateTime(2010, 10, 11, 0, 0, 0, 0, DateTimeKind.Utc)),
                                                 new Dictionary<string, string>() { { "Subject", "CN=CommonName" } },
                                                 File.ReadAllBytes(@"D:\Temp\DevelopmentCert.pfx"),
                                                 "password",
                                                 "CSR-PEM"),
                                         };

            // Building and writing code...
            var database = new DeploymentDatabase
            {
                ConnectionSettings = new MongoConnectionSettings
                {
                    Server = server,
                    Database = databaseName,
                    Credentials = new Credentials
                    {
                        User = databaseUser,
                        Password = databasePassword,
                    },
                },
            };

            var certificates = BuildCertificates(certificatesToLoad, new CertificateLocator("323423423", false));

            var writer = CertificateManagementFactory.CreateWriter(new CertificateManagementConfigurationDatabase { Database = database });
            foreach (var certificate in certificates)
            {
                await writer.PersistCertificateAsync(certificate);
            }
        }

        [Fact(Skip = "Debug test designed to aid in setting up dependent items for deploying.")]
        public static void Debug_CreateCertificateFile()
        {
            // this is because the passwords are encrypted, so this is just a convenience test for generating a cert file

            // File path to write the new certificate retriever to
            var certificateRetrieverFilePath = @"D:\Temp\Certificates-Development.json";

            var certificatesToLoad = new[]
                                         {
                                             new CertificateDescriptionWithClearPfxPayload(
                                                 "DevelopmentCertificate",
                                                 "ThumbprintOfCertificate",
                                                 new UtcDateTimeRangeInclusive(
                                                     new DateTime(2010, 10, 10, 0, 0, 0, 0, DateTimeKind.Utc),
                                                     new DateTime(2010, 10, 11, 0, 0, 0, 0, DateTimeKind.Utc)),
                                                 new Dictionary<string, string>() { { "Subject", "CN=CommonName" } },
                                                 File.ReadAllBytes(@"D:\Temp\DevelopmentCert.pfx"),
                                                 "password",
                                                 "CSR-PEM"),
                                         };

            // Building and writing code...
            var certificates = BuildCertificates(certificatesToLoad, new CertificateLocator("323423423", false));
            var classToSave = new CertificateCollection { Certificates = certificates };
            var jsonText = classToSave.ToJson();
            File.WriteAllText(certificateRetrieverFilePath, jsonText);
        }

        [Fact(Skip = "Debug test designed to aid in setting up dependent items for deploying.")]
        public static async Task Debug_ReadCertificate()
        {
            var server = "ServerOrIpOrDns";
            var databaseName = "Database";
            var databaseUser = "User";
            var databasePassword = "Password";
            var certName = "CertificateToQueryFor";

            var database = new DeploymentDatabase
                               {
                                   ConnectionSettings = new MongoConnectionSettings
                                                            {
                                                                Server = server,
                                                                Database = databaseName,
                                                                Credentials = new Credentials
                                                                                  {
                                                                                      User = databaseUser,
                                                                                      Password = databasePassword.ToSecureString(),
                                                                                  },
                                                            },
                               };

            var certReader = CertificateRetrieverFromMongo.Build(database);
            var result = await certReader.GetCertificateByNameAsync(certName);
        }

        [Fact]
        //[Fact(Skip = "Debug test designed to aid in setting up dependent items for deploying.")]
        public static async Task Debug_ReadInstances()
        {
            //var server = "deployment.database.legacy.cometrics.com";
            var server = "deployment.database.production-1.cometrics.com";
            var databaseName = "Deployment";
            var databaseUser = "sa";
            //var databasePassword = "xB$Z8BmLgTHs7jKPngp"; // legacy
            var databasePassword = "4ceMSj0dWyXbQdcb"; // prod-1

            var database = new DeploymentDatabase
                               {
                                   ConnectionSettings = new MongoConnectionSettings
                                                            {
                                                                Server = server,
                                                                Database = databaseName,
                                                                Credentials = new Credentials
                                                                                  {
                                                                                      User = databaseUser,
                                                                                      Password = databasePassword.ToSecureString(),
                                                                                  },
                                                            },
                               };

            var instanceQueries = database.GetQueriesInterface<InstanceContainer>();
            var deployedInstances = await instanceQueries.GetAllAsync();
        }

        private static List<CertificateDescriptionWithEncryptedPfxPayload> BuildCertificates(IList<CertificateDescriptionWithClearPfxPayload> certificatesToLoad, CertificateLocator encryptingCertificateLocator)
        {
            var certificates = new List<CertificateDescriptionWithEncryptedPfxPayload>();

            foreach (var certificateToLoad in certificatesToLoad)
            {
                var certificateToAdd = certificateToLoad.ToEncryptedVersion(encryptingCertificateLocator);
                certificates.Add(certificateToAdd);
            }

            return certificates;
        }
    }
}
