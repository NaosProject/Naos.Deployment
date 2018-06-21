// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.EventLog.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;

    using OBeautifulCode.Validation.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private List<SetupStep> GetCreateEventLogSpecificSteps(InitializationStrategyCreateEventLog eventLogToCreateStrategy, string packageId)
        {
            var eventLogSteps = new List<SetupStep>();

            var logName = eventLogToCreateStrategy.LogName;
            var sources = eventLogToCreateStrategy.Source;

            new { logName }.Must().NotBeNullNorWhiteSpace();
            new { sources }.Must().NotBeNullNorEmptyEnumerableNorContainAnyNulls();

            var createEventLogParams = new object[] { logName, sources };

            eventLogSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"Creating EventLog '{logName}' for Source '{sources}' for '{packageId}'."),
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(this.Settings.DeploymentScriptBlocks.CreateEventLog.ScriptText, createEventLogParams).ToList(),
                    });

            return eventLogSteps;
        }
    }
}
