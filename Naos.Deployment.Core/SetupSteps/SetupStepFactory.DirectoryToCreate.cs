// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.DirectoryToCreate.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private List<SetupStep> GetDirectoryToCreateSpecificSteps(InitializationStrategyDirectoryToCreate directoryToCreateStrategy, string packageId, Func<string, string> funcToReplaceTokensInReplacementValue)
        {
            var dir = directoryToCreateStrategy.DirectoryToCreate;
            var fullControlAccount = funcToReplaceTokensInReplacementValue(dir.FullControlAccount);
            var fullPath = funcToReplaceTokensInReplacementValue(dir.FullPath);

            var dirParams = new object[] { fullPath, fullControlAccount };
            var ret = new SetupStep
            {
                Description = Invariant($"Creating directory '{fullPath}' with full control granted to '{fullControlAccount}' for '{packageId}'."),
                SetupFunc =
                    machineManager =>
                    machineManager.RunScript(
                        this.Settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl
                        .ScriptText,
                        dirParams).ToList(),
            };

            return new[] { ret }.ToList();
        }
    }
}
