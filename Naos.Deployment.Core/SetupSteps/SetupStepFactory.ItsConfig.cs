// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.ItsConfig.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "applicationRootPath", Justification = "Keeping in signature for availability.")]
        private List<SetupStep> GetItsConfigSteps(
            IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides,
            LogWritingSettings logWritingSettings,
            string applicationRootPath,
            string environment,
            string configFilePath)
        {
            var itsConfigSteps = new List<SetupStep>();

            var updateExeConfigScriptBlock = this.Settings.DeploymentScriptBlocks.UpdateItsConfigPrecedence;
            var precedenceChain = this.configFileManager.BuildPrecedenceChain(environment);
            var updateExeConfigScriptParams = new object[] { configFilePath, precedenceChain.ToArray() };

            itsConfigSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"Update Its.Config precedence: {string.Join("|", precedenceChain)}."),
                        SetupFunc = machineManager => machineManager.RunScript(updateExeConfigScriptBlock.ScriptText, updateExeConfigScriptParams).ToList(),
                    });

            var logWritingSettingsFilePath = this.configFileManager.BuildConfigPath(applicationRootPath, fileNameWithExtension: Invariant($"{nameof(LogWritingSettings)}.json"));
            var logWritingSettingsFileContents = this.configFileManager.SerializeConfigToFileText(logWritingSettings);
            var logWritingSettingsBytes = this.configFileManager.ConvertConfigFileTextToFileBytes(logWritingSettingsFileContents);
            itsConfigSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"(Over)write default Its.Config logging file: {nameof(logWritingSettings)}."),
                        SetupFunc = machineManager =>
                            {
                                machineManager.SendFile(logWritingSettingsFilePath, logWritingSettingsBytes, false, true);
                                return new dynamic[0];
                            },
                    });

            foreach (var itsConfigOverride in itsConfigOverrides ?? new List<ItsConfigOverride>())
            {
                var itsFilePath = this.configFileManager.BuildConfigPath(
                    applicationRootPath,
                    environment,
                    Invariant($"{itsConfigOverride.FileNameWithoutExtension}.json"));

                var itsFileBytes = this.configFileManager.ConvertConfigFileTextToFileBytes(itsConfigOverride.FileContentsJson);

                itsConfigSteps.Add(
                    new SetupStep
                        {
                            Description = Invariant($"(Over)write Its.Config file: {itsConfigOverride.FileNameWithoutExtension}."),
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
