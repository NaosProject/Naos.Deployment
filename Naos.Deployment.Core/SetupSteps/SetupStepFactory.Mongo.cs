// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.Mongo.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Contract;

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
                        SetupAction =
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
                        SetupAction =
                            machineManager =>
                            machineManager.RunScript(createLogDirScript.ScriptText, createLogDirParams)
                    });

            var installMongoScript = this.settings.DeploymentScriptBlocks.InstallChocolatey;
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
                        SetupAction =
                            machineManager =>
                            machineManager.RunScript(installMongoScript.ScriptText, installMongoParams)
                    });

            var setupMongoAsServiceScript = this.settings.DeploymentScriptBlocks.SetupMongoAsService;
            var setupMongoAsServiceParams = new[] { mongoServiceAccount, dataDirectory, logDirectory };
            mongoSteps.Add(
                new SetupStep
                {
                    Description = "Setup Mongo as service.",
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(setupMongoAsServiceScript.ScriptText, setupMongoAsServiceParams)
                });

            var restartMongoServerScript = this.settings.DeploymentScriptBlocks.RestartWindowsService;
            var restartMongoServerParams = new[] { this.settings.MongoServerSettings.ServiceName };
            mongoSteps.Add(
                new SetupStep
                {
                    Description = "Restart Mongo service for change(s) to take effect.",
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(restartMongoServerScript.ScriptText, restartMongoServerParams)
                });

            return mongoSteps;
        }
    }
}
