// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.EventLog.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;

    using Naos.Deployment.Domain;

    using Spritely.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private List<SetupStep> GetCreateEventLogSpecificSteps(InitializationStrategyCreateEventLog eventLogToCreateStrategy)
        {
            var eventLogSteps = new List<SetupStep>();

            var logName = eventLogToCreateStrategy.LogName;
            var sources = eventLogToCreateStrategy.Source;

            new { logName }.Must().NotBeNull().And().NotBeWhiteSpace().OrThrowFirstFailure();
            new { source = sources }.Must().NotBeNull().And().NotBeEmptyEnumerable<string>().OrThrowFirstFailure();

            var createEventLogParams = new object[] { logName, sources };

            eventLogSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"Creating EventLog '{logName}' for Source '{sources}'"),
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(this.settings.DeploymentScriptBlocks.CreateEventLog.ScriptText, createEventLogParams),
                    });

            return eventLogSteps;
        }
    }
}
