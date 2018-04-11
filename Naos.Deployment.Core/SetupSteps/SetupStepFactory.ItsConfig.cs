// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.ItsConfig.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
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
    internal partial class SetupStepFactory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "applicationRootPath", Justification = "Keeping in signature for availability.")]
        private List<SetupStep> GetItsConfigSteps(
            IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides,
            LogProcessorSettings logProcessorSettings,
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
                        SetupFunc = machineManager => machineManager.RunScript(updateExeConfigScriptBlock.ScriptText, updateExeConfigScriptParams),
                    });

            var logProcessorSettingsFilePath = this.configFileManager.BuildConfigPath(applicationRootPath, fileNameWithExtension: Invariant($"{nameof(LogProcessorSettings)}.json"));
            var logProcessorSettingsFileContents = this.configFileManager.SerializeConfigToFileText(logProcessorSettings);
            var logProcessorSettingsBytes = this.configFileManager.ConvertConfigFileTextToFileBytes(logProcessorSettingsFileContents);
            itsConfigSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"(Over)write default Its.Config logging file: {nameof(LogProcessorSettings)}."),
                        SetupFunc = machineManager =>
                            {
                                machineManager.SendFile(logProcessorSettingsFilePath, logProcessorSettingsBytes, false, true);
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
