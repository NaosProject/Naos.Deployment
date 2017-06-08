// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DebugSeeder.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Deployment.Core.CertificateManagement;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.Domain;

    using Spritely.ReadModel.Mongo;
    using Spritely.Recipes;

    using Xunit;

    using Credentials = Spritely.ReadModel.Credentials;

    public class DebugSeeder
    {
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
                        Password = databasePassword
                    }
                }
            };

            BsonClassMapManager.RegisterClassMaps();

            var arcologyInfoCommands = database.GetCommandsInterface<string, ArcologyInfoContainer>();
            var arcologyInfoContainer = new ArcologyInfoContainer
                                            {
                                                Id = databaseId,
                                                Environment = environment,
                                                ArcologyInfo = arcologyInfo
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
                                             new CertificateToLoad(
                                                 "DevelopmentCertificate",
                                                 @"D:\Temp\DevelopmentCert.pfx",
                                                 "password",
                                                 new CertificateLocator("323423423", false))
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
                        Password = databasePassword
                    }
                }
            };

            var certificates = BuildCertificates(certificatesToLoad);

            var writer = CertificateManagementFactory.CreateWriter(new CertificateManagementConfigurationDatabase { Database = database });
            foreach (var certificate in certificates)
            {
                await writer.LoadCertficateAsync(certificate);
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
                                             new CertificateToLoad(
                                                 "DevelopmentCertificate",
                                                 @"D:\Temp\DevelopmentCert.pfx",
                                                 "password",
                                                 new CertificateLocator("323423423", false))
                                         };

            // Building and writing code...
            var certificates = BuildCertificates(certificatesToLoad);
            var classToSave = new CertificateCollection { Certificates = certificates };
            var jsonText = classToSave.ToJson();
            File.WriteAllText(certificateRetrieverFilePath, jsonText);
        }

        private static List<CertificateDetails> BuildCertificates(IList<CertificateToLoad> certificatesToLoad)
        {
            var certificates = new List<CertificateDetails>();

            foreach (var certificateToLoad in certificatesToLoad)
            {
                var certificateToAdd = certificateToLoad.ToCertificateDetails();
                certificates.Add(certificateToAdd);
            }

            return certificates;
        }
    }
}
