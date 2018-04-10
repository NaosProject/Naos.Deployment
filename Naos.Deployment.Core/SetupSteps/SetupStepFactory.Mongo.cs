// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.Mongo.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private List<SetupStep> GetInstallMongoSteps()
        {
            var mongoInstallSteps = new List<SetupStep>();

            var installMongoScript = this.Settings.DeploymentScriptBlocks.InstallChocolateyPackages;
            var installMongoParams =
                new[]
                    {
                        this.Settings.MongoServerSettings.MongoServerPackage,
                        this.Settings.MongoServerSettings.MongoClientPackage,
                    }.Select(_ => _ as object).ToArray();

            mongoInstallSteps.Add(
                new SetupStep
                    {
                        Description = "Install Mongo Server and Client.",
                        SetupFunc =
                            machineManager =>
                                machineManager.RunScript(installMongoScript.ScriptText, installMongoParams),
                    });

            var openPortParamsOne = new[] { "27017", "Allow TCP 27017 IN for Mongo" };
            mongoInstallSteps.Add(
                new SetupStep
                    {
                        Description = $"Open port 27017 for Mongo",
                        SetupFunc =
                            machineManager =>
                                machineManager.RunScript(this.Settings.DeploymentScriptBlocks.OpenPort.ScriptText, openPortParamsOne),
                    });

            var openPortParamsTwo = new[] { "27018", "Allow TCP 27018 IN for Mongo" };
            mongoInstallSteps.Add(
                new SetupStep
                    {
                        Description = $"Open port 27018 for Mongo",
                        SetupFunc =
                            machineManager =>
                                machineManager.RunScript(this.Settings.DeploymentScriptBlocks.OpenPort.ScriptText, openPortParamsTwo),
                    });

            return mongoInstallSteps;
        }

        private List<SetupStep> GetConfigureMongoSteps(InitializationStrategyMongo mongoStrategy)
        {
            var mongoSteps = new List<SetupStep>();
            var mongoServiceAccount = this.Settings.MongoServerSettings.ServiceAccount;

            var dataDirectory = mongoStrategy.DataDirectory ?? this.Settings.MongoServerSettings.DefaultDataDirectory;
            var createDatabaseDirScript = this.Settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createDatabaseDirParams = new[] { dataDirectory, mongoServiceAccount };
            mongoSteps.Add(
                new SetupStep
                    {
                        Description = "Create " + dataDirectory + " and grant rights to Mongo service account.",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(
                                createDatabaseDirScript.ScriptText,
                                createDatabaseDirParams),
                    });

            var logDirectory = mongoStrategy.LogDirectory ?? this.Settings.MongoServerSettings.DefaultLogDirectory;
            var createLogDirScript = this.Settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createLogDirParams = new[] { logDirectory, mongoServiceAccount };
            mongoSteps.Add(
                new SetupStep
                    {
                        Description = "Create " + logDirectory + " and grant rights to Mongo service account.",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(createLogDirScript.ScriptText, createLogDirParams),
                    });

            var backupDirectory = mongoStrategy.BackupDirectory ?? this.Settings.MongoServerSettings.DefaultBackupDirectory;
            var createBackupDirScript = this.Settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createBackupDirParams = new[] { backupDirectory, mongoServiceAccount };
            mongoSteps.Add(
                new SetupStep
                    {
                        Description = "Create " + backupDirectory + " and grant rights to Mongo service account.",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(createBackupDirScript.ScriptText, createBackupDirParams),
                    });

            var configureMongoScript = this.Settings.DeploymentScriptBlocks.ConfigureMongo;
            var configureMongoParams = new object[] { mongoStrategy.DocumentDatabaseName, mongoStrategy.AdministratorPassword, dataDirectory, logDirectory, mongoStrategy.NoJournaling };
            mongoSteps.Add(
                new SetupStep
                {
                    Description = "Configure Mongo.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(configureMongoScript.ScriptText, configureMongoParams),
                });

            var restartMongoServerScript = this.Settings.DeploymentScriptBlocks.RestartWindowsService;
            var restartMongoServerParams = new[] { this.Settings.MongoServerSettings.ServiceName };
            mongoSteps.Add(
                new SetupStep
                {
                    Description = "Restart Mongo service for change(s) to take effect.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(restartMongoServerScript.ScriptText, restartMongoServerParams),
                });

            return mongoSteps;
        }

        private static void ThrowIfMultipleMongoStrategiesAreInvalidCombination(IReadOnlyCollection<InitializationStrategyMongo> mongoStrategies)
        {
            // Make sure we only have one data,log path, and journaling option because mongo uses a single config file for this
            var distinctNoJournaling = mongoStrategies.Select(_ => _.NoJournaling).Distinct();
            if (distinctNoJournaling.Count() > 1)
            {
                throw new ArgumentException("Cannot have multiple no journaling options for a single mongo instance deployment.");
            }

            var distinctDataPath = mongoStrategies.Select(_ => _.DataDirectory).Distinct();
            if (distinctDataPath.Count() > 1)
            {
                throw new ArgumentException("Cannot have multiple data paths for a single mongo instance deployment.");
            }

            var distinctLogPath = mongoStrategies.Select(_ => _.LogDirectory).Distinct();
            if (distinctLogPath.Count() > 1)
            {
                throw new ArgumentException("Cannot have multiple log paths for a single mongo instance deployment.");
            }
        }
    }
}
