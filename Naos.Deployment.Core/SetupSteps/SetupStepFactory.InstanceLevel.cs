// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.InstanceLevel.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.Packaging.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        /// <summary>
        /// Gets the instance level setup steps.
        /// </summary>
        /// <param name="computerName">Computer name to use in windows for the instance.</param>
        /// <param name="windowsSku">The windows SKU used to create the instance.</param>
        /// <param name="environment">Environment the instance is in.</param>
        /// <param name="allInitializationStrategies">All initialization strategies to be setup.</param>
        /// <param name="workingDirectoryForFileCopyDuringInstanceSetup">Path to store files temporarily.</param>
        /// <param name="funcToReplaceKnownTokensWithValues">Function to replace well known tokens with values.</param>
        /// <returns>List of setup steps </returns>
        public async Task<IReadOnlyCollection<SetupStepBatch>> GetInstanceLevelSetupSteps(string computerName, WindowsSku windowsSku, string environment, IReadOnlyCollection<InitializationStrategyBase> allInitializationStrategies, string workingDirectoryForFileCopyDuringInstanceSetup, Func<string, string> funcToReplaceKnownTokensWithValues)
        {
            var steps = new List<SetupStep>();

            var setupWinRm = new SetupStep
                                 {
                                     Description = "Setup WinRM.",
                                     SetupFunc =
                                         machineManager =>
                                         machineManager.RunScript(
                                             this.Settings.DeploymentScriptBlocks.SetupWinRmScript
                                             .ScriptText).ToList(),
                                 };

            steps.Add(setupWinRm);

            var setupUpdates = new SetupStep
                                   {
                                       Description = "Setup Windows Updates.",
                                       SetupFunc =
                                           machineManager =>
                                           machineManager.RunScript(
                                               this.Settings.DeploymentScriptBlocks
                                               .SetupWindowsUpdatesScript.ScriptText).ToList(),
                                   };

            steps.Add(setupUpdates);

            var setupTime = new SetupStep
                                {
                                    Description = "Setup Windows Time.",
                                    SetupFunc =
                                        machineManager =>
                                        machineManager.RunScript(
                                            this.Settings.DeploymentScriptBlocks.SetupWindowsTimeScript.ScriptText).ToList(),
                                };

            steps.Add(setupTime);

            var execScripts = new SetupStep
                                  {
                                      Description = "Enable Script Execution.",
                                      SetupFunc =
                                          machineManager =>
                                          machineManager.RunScript(
                                              this.Settings.DeploymentScriptBlocks
                                              .EnableScriptExecutionScript.ScriptText).ToList(),
                                  };

            steps.Add(execScripts);

            var fullComputerNameEnvironmentVariable = "FullComputerName";
            var windowsSkuEnvironmentVariable = "WindowsSku";
            var addEnvironmentVariables = new SetupStep
                                              {
                                                  Description = "Add Machine Level Environment Variables.",
                                                  SetupFunc = machineManager =>
                                                      {
                                                          var environmentVariablesToAdd = new[]
                                                                                                  {
                                                                                                      new
                                                                                                          {
                                                                                                              Name = this.Settings.EnvironmentEnvironmentVariableName,
                                                                                                              Value = environment,
                                                                                                          },
                                                                                                      new
                                                                                                          {
                                                                                                              Name = windowsSkuEnvironmentVariable,
                                                                                                              Value = windowsSku.ToString(),
                                                                                                          },
                                                                                                      new
                                                                                                          {
                                                                                                              Name = fullComputerNameEnvironmentVariable,
                                                                                                              Value = computerName,
                                                                                                          },
                                                                                                  };
                                                          return
                                                              machineManager.RunScript(
                                                                  this.Settings.DeploymentScriptBlocks.AddMachineLevelEnvironmentVariables.ScriptText,
                                                                  new[] { environmentVariablesToAdd }).ToList();
                                                      },
                                              };

            steps.Add(addEnvironmentVariables);

            var wallpaperUpdate = new SetupStep
                                      {
                                          Description = "Customize Instance Wallpaper.",
                                          SetupFunc = machineManager =>
                                              {
                                                  var environmentVariablesToAddToWallpaper = new[] { this.Settings.EnvironmentEnvironmentVariableName, windowsSkuEnvironmentVariable, fullComputerNameEnvironmentVariable };
                                                  return
                                                      machineManager.RunScript(
                                                          this.Settings.DeploymentScriptBlocks.UpdateInstanceWallpaper.ScriptText,
                                                          new[] { environmentVariablesToAddToWallpaper }).ToList();
                                              },
                                      };

            steps.Add(wallpaperUpdate);

            var registryKeysToUpdateExplorer = new[]
                                                   {
                                                       new
                                                           {
                                                               Path = "Registry::HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                                                               Name = "Hidden",
                                                               Value = "1",
                                                               Type = "DWord",
                                                           },
                                                       new
                                                           {
                                                               Path = "Registry::HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                                                               Name = "ShowSuperHidden",
                                                               Value = "1",
                                                               Type = "DWord",
                                                           },
                                                       new
                                                           {
                                                               // http://superuser.com/questions/666891/script-to-set-hide-file-extensions
                                                               Path = "Registry::HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                                                               Name = "HideFileExt",
                                                               Value = "0",
                                                               Type = "DWord",
                                                           },
                                                   };

            var explorerShowHidden = new SetupStep
                                         {
                                             Description = "Set Explorer to show all hidden files with extensions.",
                                             SetupFunc = machineManager =>
                                                 {
                                                     var fileExplorerParams = new[] { registryKeysToUpdateExplorer };
                                                     return
                                                         machineManager.RunScript(
                                                             this.Settings.DeploymentScriptBlocks.UpdateWindowsRegistryEntries.ScriptText,
                                                             fileExplorerParams).ToList();
                                                 },
                                         };

            steps.Add(explorerShowHidden);

            if (!string.IsNullOrEmpty(this.environmentCertificateName))
            {
                var distinctInitializationStrategyTypes = allInitializationStrategies.Select(_ => _.GetType()).ToList();
                if (distinctInitializationStrategyTypes.Any(_ => this.InitializationStrategyTypesThatNeedEnvironmentCertificate.Contains(_)))
                {
                    var usersToGrantAccessToKey = allInitializationStrategies.Select(this.GetAccountToUse).Where(_ => _ != null).Distinct().ToArray();

                    var environmentCertSteps = await this.GetCertificateToInstallSpecificStepsParameterizedWithoutStrategyAsync(
                                                   "Instance",
                                                   workingDirectoryForFileCopyDuringInstanceSetup,
                                                   funcToReplaceKnownTokensWithValues,
                                                   usersToGrantAccessToKey,
                                                   this.environmentCertificateName,
                                                   false);
                    steps.AddRange(environmentCertSteps);
                }
            }

            var rename = new SetupStep
                             {
                                 Description = "Rename Computer.",
                                 SetupFunc = machineManager =>
                                     {
                                         var renameParams = new[] { computerName };
                                         return machineManager.RunScript(
                                             this.Settings.DeploymentScriptBlocks.RenameComputerScript.ScriptText,
                                             renameParams).ToList();
                                     },
                             };

            steps.Add(rename);

            return new[] { new SetupStepBatch { ExecutionOrder = ExecutionOrder.InstanceLevel, Steps = steps } };
        }

        /// <summary>
        /// Gets the chocolatey setup steps.
        /// </summary>
        /// <param name="chocolateyPackages">Chocolatey packages to install.</param>
        /// <returns>List of setup steps </returns>
        public IReadOnlyCollection<SetupStepBatch> GetChocolateySetupSteps(IReadOnlyCollection<PackageDescription> chocolateyPackages)
        {
            var chocolateyIndividualSetupSteps = this.GetChocolateyIndividualSetupSteps(chocolateyPackages);
            return new[] { new SetupStepBatch { ExecutionOrder = ExecutionOrder.Chocolatey, Steps = chocolateyIndividualSetupSteps } };
        }

        private IReadOnlyCollection<SetupStep> GetChocolateyIndividualSetupSteps(IReadOnlyCollection<PackageDescription> chocolateyPackages)
        {
            var installChocoSteps = new List<SetupStep>();
            if (chocolateyPackages != null && chocolateyPackages.Any())
            {
                var installChocoClientStep = new SetupStep
                                                 {
                                                     Description = "Install Chocolatey Client.",
                                                     SetupFunc =
                                                         machineManager =>
                                                         machineManager.RunScript(this.Settings.DeploymentScriptBlocks.InstallChocolatey.ScriptText).ToList(),
                                                 };

                installChocoSteps.Add(installChocoClientStep);

                foreach (var chocoPackage in chocolateyPackages)
                {
                    var installChocoPackagesStep = new SetupStep
                                                       {
                                                           Description = Invariant($"Install Chocolatey Package: {chocoPackage.GetIdDotVersionString()}."),
                                                           SetupFunc = machineManager =>
                                                               {
                                                                   var installChocoPackageParams = new object[] { chocoPackage };
                                                                   return
                                                                       machineManager.RunScript(
                                                                           this.Settings.DeploymentScriptBlocks.InstallChocolateyPackages.ScriptText,
                                                                           installChocoPackageParams).ToList();
                                                               },
                                                       };

                    installChocoSteps.Add(installChocoPackagesStep);
                }
            }

            return installChocoSteps;
        }
    }
}
