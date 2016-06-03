// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.InstanceLevel.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;

    using Naos.Packaging.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        /// <summary>
        /// Gets the instance level setup steps.
        /// </summary>
        /// <param name="computerName">Computer name to use in windows for the instance.</param>
        /// <param name="chocolateyPackages">Chocolatey packages to install.</param>
        /// <returns>List of setup steps </returns>
        public ICollection<SetupStep> GetInstanceLevelSetupSteps(string computerName, IReadOnlyCollection<PackageDescription> chocolateyPackages)
        {
            var ret = new List<SetupStep>();

            var setupWinRm = new SetupStep
                                 {
                                     Description = "Setup WinRM",
                                     SetupAction =
                                         machineManager =>
                                         machineManager.RunScript(
                                             this.settings.DeploymentScriptBlocks.SetupWinRmScriptBlock
                                             .ScriptText)
                                 };

            ret.Add(setupWinRm);

            var setupUpdates = new SetupStep
                                   {
                                       Description = "Setup Windows Updates",
                                       SetupAction =
                                           machineManager =>
                                           machineManager.RunScript(
                                               this.settings.DeploymentScriptBlocks
                                               .SetupWindowsUpdatesScriptBlock.ScriptText)
                                   };

            ret.Add(setupUpdates);

            var setupTime = new SetupStep
                                {
                                    Description = "Setup Windows Time",
                                    SetupAction =
                                        machineManager =>
                                        machineManager.RunScript(
                                            this.settings.DeploymentScriptBlocks.SetupWindowsTimeScriptBlock
                                            .ScriptText)
                                };

            ret.Add(setupTime);

            var execScripts = new SetupStep
                                  {
                                      Description = "Enable Script Execution",
                                      SetupAction =
                                          machineManager =>
                                          machineManager.RunScript(
                                              this.settings.DeploymentScriptBlocks
                                              .EnableScriptExecutionScriptBlock.ScriptText)
                                  };

            ret.Add(execScripts);

            var installChocoSteps = this.GetChocolateySetupSteps(chocolateyPackages);
            ret.AddRange(installChocoSteps);

            var rename = new SetupStep
                             {
                                 Description = "Rename Computer",
                                 SetupAction = machineManager =>
                                     {
                                         var renameScript =
                                             this.settings.DeploymentScriptBlocks.RenameComputerScriptBlock
                                                 .ScriptText;
                                         var renameParams = new[] { computerName };
                                         machineManager.RunScript(renameScript, renameParams);
                                     }
                             };

            ret.Add(rename);

            return ret;
        }
    }
}
