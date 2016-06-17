// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.ItsConfig.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private List<SetupStep> GetItsConfigSteps(
            ICollection<ItsConfigOverride> itsConfigOverrides,
            string applicationRootPath,
            string environment,
            string configFilePath)
        {
            var itsConfigSteps = new List<SetupStep>();
            var updateExeConfigScriptBlock = this.settings.DeploymentScriptBlocks.UpdateItsConfigPrecedence;
            var precedenceChain = new[] { environment }.ToList();
            precedenceChain.AddRange(this.itsConfigPrecedenceAfterEnvironment);
            var updateExeConfigScriptParams = new object[] { configFilePath, precedenceChain.ToArray() };

            itsConfigSteps.Add(
                new SetupStep
                    {
                        Description = "Update Its.Config precedence: " + string.Join("|", precedenceChain),
                        SetupAction = machineManager => machineManager.RunScript(updateExeConfigScriptBlock.ScriptText, updateExeConfigScriptParams)
                    });

            foreach (var itsConfigOverride in itsConfigOverrides ?? new List<ItsConfigOverride>())
            {
                var itsFileSubPath = $".config/{environment}/{itsConfigOverride.FileNameWithoutExtension}.json";

                var itsFilePath = Path.Combine(applicationRootPath, itsFileSubPath);
                var itsFileBytes = Encoding.UTF8.GetBytes(itsConfigOverride.FileContentsJson);

                itsConfigSteps.Add(
                    new SetupStep
                        {
                            Description = "(Over)write Its.Config file: " + itsConfigOverride.FileNameWithoutExtension,
                            SetupAction = machineManager => machineManager.SendFile(itsFilePath, itsFileBytes, false, true)
                        });
            }

            return itsConfigSteps;
        }
    }
}
