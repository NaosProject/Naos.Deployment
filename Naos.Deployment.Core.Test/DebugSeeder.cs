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

    using Naos.Deployment.Core.CertificateManagement;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;
    using Naos.Deployment.Tracking;

    using Spritely.ReadModel;
    using Spritely.ReadModel.Mongo;

    using Xunit;

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
            var databasePassword = WinRM.MachineManager.ConvertStringToSecureString("password");

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
        public static void Debug_CreateCertificateEntryInDatabase()
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
            var databasePassword = WinRM.MachineManager.ConvertStringToSecureString("password");

            var certificatesToLoad = new[]
                                         {
                                             new CertificateToLoad
                                                 {
                                                     EncryptingCertificateThumbprint = "323423423",
                                                     EncryptingCertificateIsValid = false,
                                                     FilePath = @"D:\Temp\DevelopmentCert.pfx",
                                                     Name = "DevelopmentCertificate",
                                                     PasswordInClearText = "password"
                                                 }
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

            BsonClassMapManager.RegisterClassMaps();
            var commands = database.GetCommandsInterface<string, CertificateContainer>();
            var certificateContainer = new CertificateContainer
                                           {
                                               Id = databaseId,
                                               Environment = environment,
                                               Certificates = certificates.ToArray()
                                           };

            commands.AddOrUpdateOneAsync(certificateContainer).Wait();
        }

        [Fact(Skip = "Debug test designed to aid in setting up dependent items for deploying.")]
        public static void Debug_CreateCertificateFile()
        {
            // this is because the passwords are encrypted, so this is just a convenience test for generating a cert file

            // File path to write the new certificate retriever to
            var certificateRetrieverFilePath = @"D:\Temp\Certificates-Development.json";

            var certificatesToLoad = new[]
                                         {
                                             new CertificateToLoad
                                                 {
                                                     EncryptingCertificateThumbprint = "323423423",
                                                     EncryptingCertificateIsValid = false,
                                                     FilePath = @"D:\Temp\DevelopmentCert.pfx",
                                                     Name = "DevelopmentCertificate",
                                                     PasswordInClearText = "password"
                                                 }
                                         };

            // Building and writing code...
            var certificates = BuildCertificates(certificatesToLoad);
            var classToSave = new CertificateCollection { Certificates = certificates };
            var jsonText = Serializer.Serialize(classToSave);
            File.WriteAllText(certificateRetrieverFilePath, jsonText);
        }

        private static List<CertificateDetails> BuildCertificates(IList<CertificateToLoad> certificatesToLoad)
        {
            var certificates = new List<CertificateDetails>();

            foreach (var certificateToLoad in certificatesToLoad)
            {
                var encryptingCertificateLocator = new CertificateLocator
                                                       {
                                                           CertificateThumbprint =
                                                               certificateToLoad
                                                               .EncryptingCertificateThumbprint,
                                                           CertificateIsValid =
                                                               certificateToLoad
                                                               .EncryptingCertificateIsValid
                                                       };

                var encryptedPassword = Encryptor.Encrypt(certificateToLoad.PasswordInClearText, encryptingCertificateLocator);

                var certificateBytes = File.ReadAllBytes(certificateToLoad.FilePath);
                var certificateFileBase64 = Convert.ToBase64String(certificateBytes);
                var encryptedFileBase64 = Encryptor.Encrypt(certificateFileBase64, encryptingCertificateLocator);

                var certificateToAdd = new CertificateDetails
                                           {
                                               Name = certificateToLoad.Name,
                                               EncryptedBase64Bytes = encryptedFileBase64,
                                               EncryptedPassword = encryptedPassword,
                                               EncryptingCertificateLocator = encryptingCertificateLocator
                                           };

                certificates.Add(certificateToAdd);
            }

            return certificates;
        }

        private class CertificateToLoad
        {
            /// <summary>
            /// Gets or sets the name of the certificate.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the local path of the certificate file.
            /// </summary>
            public string FilePath { get; set; }

            /// <summary>
            /// Gets or sets the certificate file password in clear text (to be encrypted).
            /// </summary>
            public string PasswordInClearText { get; set; }

            /// <summary>
            /// Gets or sets the certificate thumbprint of a certificate on the running machine that will be used to encrypt the password.
            /// </summary>
            public string EncryptingCertificateThumbprint { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether or not the certificate being used to encrypt the password is "valid".
            /// </summary>
            public bool EncryptingCertificateIsValid { get; set; }
        }
    }
}
