// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.Mongo.cs" company="Naos">
//   Copyright 2015 Naos
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
    public partial class SetupStepFactory
    {
        private List<SetupStep> GetMongoSpecificSteps(InitializationStrategyMongo mongoStrategy)
        {
            var mongoSteps = new List<SetupStep>();
            var mongoServiceAccount = this.settings.MongoServerSettings.ServiceAccount;

            var dataDirectory = mongoStrategy.DataDirectory ?? this.settings.MongoServerSettings.DefaultDataDirectory;
            var createDatabaseDirScript = this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createDatabaseDirParams = new[] { dataDirectory, mongoServiceAccount };
            mongoSteps.Add(
                new SetupStep
                    {
                        Description = "Create " + dataDirectory + " and grant rights to Mongo service account.",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(
                                createDatabaseDirScript.ScriptText,
                                createDatabaseDirParams)
                    });

            var logDirectory = mongoStrategy.LogDirectory ?? this.settings.MongoServerSettings.DefaultLogDirectory;
            var createLogDirScript = this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl;
            var createLogDirParams = new[] { logDirectory, mongoServiceAccount };
            mongoSteps.Add(
                new SetupStep
                    {
                        Description = "Create " + logDirectory + " and grant rights to Mongo service account.",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(createLogDirScript.ScriptText, createLogDirParams)
                    });

            var installMongoScript = this.settings.DeploymentScriptBlocks.InstallChocolateyPackages;
            var installMongoParams =
                new[]
                    {
                        this.settings.MongoServerSettings.MongoServerPackage,
                        this.settings.MongoServerSettings.MongoClientPackage
                    }.Select(_ => _ as object).ToArray();

            mongoSteps.Add(
                new SetupStep
                    {
                        Description = "Install Mongo Server and Client.",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(installMongoScript.ScriptText, installMongoParams)
                    });

            var configureMongoScript = this.settings.DeploymentScriptBlocks.ConfigureMongo;
            var configureMongoParams = new object[] { mongoStrategy.DocumentDatabaseName, mongoStrategy.AdministratorPassword, dataDirectory, logDirectory, mongoStrategy.NoJournaling };
            mongoSteps.Add(
                new SetupStep
                {
                    Description = "Configure Mongo.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(configureMongoScript.ScriptText, configureMongoParams)
                });

            var restartMongoServerScript = this.settings.DeploymentScriptBlocks.RestartWindowsService;
            var restartMongoServerParams = new[] { this.settings.MongoServerSettings.ServiceName };
            mongoSteps.Add(
                new SetupStep
                {
                    Description = "Restart Mongo service for change(s) to take effect.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(restartMongoServerScript.ScriptText, restartMongoServerParams)
                });

            var openPortParamsOne = new[] { "27017", "Allow TCP 27017 IN for Self Hosting" };
            mongoSteps.Add(
                new SetupStep
                {
                    Description = $"Open port 27017 for Mongo",
                    SetupFunc =
                            machineManager =>
                            machineManager.RunScript(this.settings.DeploymentScriptBlocks.OpenPort.ScriptText, openPortParamsOne)
                });
            
            var openPortParamsTwo = new[] { "27018", "Allow TCP 27018 IN for Self Hosting" };
            mongoSteps.Add(
                new SetupStep
                {
                    Description = $"Open port 27018 for Mongo",
                    SetupFunc =
                            machineManager =>
                            machineManager.RunScript(this.settings.DeploymentScriptBlocks.OpenPort.ScriptText, openPortParamsTwo)
                });

            return mongoSteps;
        }

        private static void ThrowIfMultipleMongoStrategiesAreInvalidCombination(ICollection<InitializationStrategyMongo> mongoStrategies)
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
