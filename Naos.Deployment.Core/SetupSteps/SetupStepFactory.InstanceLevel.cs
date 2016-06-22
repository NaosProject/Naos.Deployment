// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.InstanceLevel.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
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
        /// <param name="allInitializationStrategies">All initialization strategies to be setup.</param>
        /// <returns>List of setup steps </returns>
        public async Task<ICollection<SetupStep>> GetInstanceLevelSetupSteps(string computerName, IReadOnlyCollection<PackageDescription> chocolateyPackages, IReadOnlyCollection<InitializationStrategyBase> allInitializationStrategies)
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

            if (!string.IsNullOrEmpty(this.environmentCertificateName))
            {
                var distinctInitializationStrategyTypes = allInitializationStrategies.Select(_ => _.GetType()).ToList();
                if (distinctInitializationStrategyTypes.Any(_ => this.InitializationStrategyTypesThatNeedEnvironmentCertificate.Contains(_)))
                {
                    var usersToGrantAccessToKey = allInitializationStrategies.Select(this.GetAccountToUse).Where(_ => _ != null).Distinct().ToArray();

                    var environmentCertSteps =
                        await
                        this.GetCertificateToInstallSpecificStepsParameterizedWithoutStrategyAsync(
                            this.RootDeploymentPath,
                            this.settings.HarnessSettings.HarnessAccount,
                            this.settings.WebServerSettings.IisAccount,
                            usersToGrantAccessToKey,
                            this.environmentCertificateName);
                    ret.AddRange(environmentCertSteps);
                }
            }

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

        private ICollection<SetupStep> GetChocolateySetupSteps(IReadOnlyCollection<PackageDescription> chocolateyPackages)
        {
            var installChocoSteps = new List<SetupStep>();
            if (chocolateyPackages != null && chocolateyPackages.Any())
            {
                var installChocoClientStep = new SetupStep
                {
                    Description = "Install Chocolatey Client",
                    SetupAction = machineManager =>
                    {
                        machineManager.RunScript(this.settings.DeploymentScriptBlocks.InstallChocolatey.ScriptText);
                    }
                };

                installChocoSteps.Add(installChocoClientStep);

                foreach (var chocoPackage in chocolateyPackages)
                {
                    var installChocoPackagesStep = new SetupStep
                                                       {
                                                           Description = "Install Chocolatey Package: " + chocoPackage.GetIdDotVersionString(),
                                                           SetupAction = machineManager =>
                                                               {
                                                                   var scriptBlockParameters = new object[] { chocoPackage };

                                                                   machineManager.RunScript(
                                                                       this.settings.DeploymentScriptBlocks.InstallChocolateyPackages.ScriptText,
                                                                       scriptBlockParameters);
                                                               }
                                                       };

                    installChocoSteps.Add(installChocoPackagesStep);
                }
            }

            return installChocoSteps;
        }
    }
}
