// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.DirectoryToCreate.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private List<SetupStep> GetDirectoryToCreateSpecificSteps(InitializationStrategyDirectoryToCreate directoryToCreateStrategy, string harnessAccount, string iisAccount)
        {
            var dir = directoryToCreateStrategy.DirectoryToCreate;
            var fullControlAccount = TokenSubstitutions.GetSubstitutedStringForAccounts(dir.FullControlAccount, harnessAccount, iisAccount);
            var dirParams = new object[] { dir.FullPath, fullControlAccount };
            var ret = new SetupStep
            {
                Description =
                    "Creating directory: " + dir.FullPath + " with full control granted to: "
                    + fullControlAccount,
                SetupFunc =
                    machineManager =>
                    machineManager.RunScript(
                        this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl
                        .ScriptText,
                        dirParams),
            };

            return new[] { ret }.ToList();
        }
    }
}
