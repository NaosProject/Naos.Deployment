// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.ItsConfig.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Naos.Deployment.Domain;
    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private List<SetupStep> GetItsConfigSteps(
            IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides,
            string applicationRootPath,
            string environment,
            string configFilePath)
        {
            var itsConfigSteps = new List<SetupStep>();
            var updateExeConfigScriptBlock = this.Settings.DeploymentScriptBlocks.UpdateItsConfigPrecedence;
            var precedenceChain = new[] { environment }.ToList();
            precedenceChain.AddRange(this.itsConfigPrecedenceAfterEnvironment);
            var updateExeConfigScriptParams = new object[] { configFilePath, precedenceChain.ToArray() };

            itsConfigSteps.Add(
                new SetupStep
                    {
                        Description = "Update Its.Config precedence: " + string.Join("|", precedenceChain),
                        SetupFunc = machineManager => machineManager.RunScript(updateExeConfigScriptBlock.ScriptText, updateExeConfigScriptParams),
                    });

            foreach (var itsConfigOverride in itsConfigOverrides ?? new List<ItsConfigOverride>())
            {
                var itsFileSubPath = Invariant($".config/{environment}/{itsConfigOverride.FileNameWithoutExtension}.json");

                var itsFilePath = Path.Combine(applicationRootPath, itsFileSubPath);
                var itsFileBytes = Encoding.UTF8.GetBytes(itsConfigOverride.FileContentsJson);

                itsConfigSteps.Add(
                    new SetupStep
                        {
                            Description = "(Over)write Its.Config file: " + itsConfigOverride.FileNameWithoutExtension,
                            SetupFunc = machineManager =>
                                {
                                    machineManager.SendFile(itsFilePath, itsFileBytes, false, true);
                                    return new dynamic[0];
                                },
                        });
            }

            return itsConfigSteps;
        }
    }
}
