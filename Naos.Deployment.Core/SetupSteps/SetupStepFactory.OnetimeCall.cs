// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.OnetimeCall.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private List<SetupStep> GetOnetimeCallSpecificSteps(InitializationStrategyOnetimeCall onetimeCallStrategy, LogWritingSettings defaultLogWritingSettings, IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides, string consoleRootPath, string environment)
        {
            var exeFilePathRelativeToPackageRoot = onetimeCallStrategy.ExeFilePathRelativeToPackageRoot;
            var justification = onetimeCallStrategy.JustificationForOnetimeCall;
            var arguments = onetimeCallStrategy.Arguments;

            var onetimeCallSetupSteps = new List<SetupStep>();

            var exeFullPath = Path.Combine(consoleRootPath, exeFilePathRelativeToPackageRoot);
            var exeConfigFullPath = exeFullPath + ".config"; // App.Config should get named this.
            var applicationRootPath = Path.GetDirectoryName(exeFullPath);

            var itsConfigSteps = this.GetItsConfigSteps(itsConfigOverrides, defaultLogWritingSettings, applicationRootPath, environment, exeConfigFullPath);
            onetimeCallSetupSteps.AddRange(itsConfigSteps);

            var onetimeCallParams = new object[] { exeFullPath, arguments };
            var argumentsForDescription = arguments ?? "<no arguments>";
            var onetimeCallStep = new SetupStep
                                      {
                                          Description = Invariant($"Running one time call because {justification}; command: {exeFilePathRelativeToPackageRoot}, args: {argumentsForDescription}."),
                                          SetupFunc = machineManager => machineManager.RunScript(
                                              this.Settings.DeploymentScriptBlocks.RunOnetimeCall.ScriptText,
                                              onetimeCallParams).ToList(),
                                      };
            onetimeCallSetupSteps.Add(onetimeCallStep);

            return onetimeCallSetupSteps;
        }
    }
}
