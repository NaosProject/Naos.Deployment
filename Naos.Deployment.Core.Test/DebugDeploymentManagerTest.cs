// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DebugDeploymentManagerTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.IO;

    using Naos.AWS.Contract;
    using Naos.Deployment.Contract;

    /// <summary>
    /// TODO: Fill out the description.
    /// </summary>
    public class DebugDeploymentManagerTest
    {
        public static void DeployNewPackage()
        {
            try
            {
                var scratchPath = @"D:\Temp\WritingDeploy\";
                var trackingFilePath = Path.Combine(scratchPath, "Tracking.json");
                var unzipDirPath = Path.Combine(scratchPath, "UnzipDir");
                var environment = "staging";
                var repoConfig = new PackageRepositoryConfiguration()
                                     {
                                         Source =
                                             "https://ci.appveyor.com/nuget/cometricstech-6qkqnuln8s3o",
                                         SourceName = "CMAppVeyorAccountGallery",
                                         Username = "tech@coopmetrics.coop",
                                         Password = "cipass01",
                                     };

                var packagesToDeploy = new[] { new PackageDescription() { Id = "CoMetrics.Website", Version = null } };
                var tracker = new ComputingInfrastructureTracker(trackingFilePath);
                var key = "FILL IN USING DEBUGGER";
                var secret = "FILL IN USING DEBUGGER";
                var deviceId = "FILL IN USING DEBUGGER";
                var token = "FILL IN USING DEBUGGER";
                var credentials = new CredentialContainer()
                                                      {
                                                          CredentialType =
                                                              Enums.CredentialType.Token,
                                                          SecretAccessKey =
                                                              secret,
                                                          AccessKeyId = key,
                                                          SessionToken = "FILL IN USING DEBUGGER",
                                                          Expiration = DateTime.Now.AddDays(1),
                                                      };
                var cloudManager = new CloudInfrastructureManager(tracker).InitializeCredentials(
                    "us-east-1",
                    TimeSpan.FromDays(1),
                    key,
                    secret,
                    deviceId,
                    token); // .InitializeCredentials(credentials);

                var packageManager = new PackageManager(repoConfig, unzipDirPath);
                var defaultDeploymentConfig = new DeploymentConfiguration()
                                                  {
                                                      InstanceType = new InstanceType { VirtualCores = 2, RamInGb = 4 },
                                                      InstanceAccessibility = InstanceAccessibility.Private,
                                                      Volumes =
                                                          new[]
                                                              {
                                                                  new Volume()
                                                                      {
                                                                          DriveLetter =
                                                                              "C",
                                                                          SizeInGb = 50,
                                                                      },
                                                                  new Volume()
                                                                      {
                                                                          DriveLetter =
                                                                              "D",
                                                                          SizeInGb = 100,
                                                                      },
                                                              },
                                                  };

                var m = new DeploymentManager(tracker, cloudManager, packageManager, defaultDeploymentConfig);
                m.DeployPackages(packagesToDeploy, environment, null);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                throw;
            }
        }
    }
}
