// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.DirectoryToCreate.cs" company="Naos">
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
                SetupAction =
                    machineManager =>
                    machineManager.RunScript(
                        this.settings.DeploymentScriptBlocks.CreateDirectoryWithFullControl
                        .ScriptText,
                        dirParams)
            };

            return new[] { ret }.ToList();
        }
    }
}
