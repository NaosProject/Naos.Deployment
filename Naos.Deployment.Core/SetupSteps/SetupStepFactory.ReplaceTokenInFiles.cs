// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.ReplaceTokenInFiles.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
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
        private List<SetupStep> GetReplaceTokenInFilesSpecificSteps(InitializationStrategyReplaceTokenInFiles replaceTokenStrategy, string packageId, string rootPath, Func<string, string> funcToReplaceTokensInReplacementValue)
        {
            var fileSearchPattern = replaceTokenStrategy.FileSearchPattern;
            var token = replaceTokenStrategy.Token;
            var replacementValue = funcToReplaceTokensInReplacementValue(replaceTokenStrategy.Replacement);
            var replaceTokenParams = new[] { rootPath, fileSearchPattern, token, replacementValue };

            var replaceTokenStep = new SetupStep
                                      {
                                          Description = Invariant($"Replacing token '{token}' with '{replacementValue}' in files matching pattern '{fileSearchPattern}' for '{packageId}'."),
                                          SetupFunc = machineManager => machineManager.RunScript(
                                              this.Settings.DeploymentScriptBlocks.ReplaceTokenInFiles.ScriptText,
                                              replaceTokenParams),
                                      };

            return new[] { replaceTokenStep }.ToList();
        }
    }
}
