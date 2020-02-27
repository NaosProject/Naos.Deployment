// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.EventLog.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;

    using OBeautifulCode.Assertion.Recipes;

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
            var source = eventLogToCreateStrategy.Source;

            new { logName }.AsArg().Must().NotBeNullNorWhiteSpace();
            new { source }.AsArg().Must().NotBeNullNorWhiteSpace();

            var createEventLogParams = new object[] { logName, source };

            eventLogSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"Creating EventLog '{logName}' for Source '{source}' for '{packageId}'."),
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(this.Settings.DeploymentScriptBlocks.CreateEventLog.ScriptText, createEventLogParams).ToList(),
                    });

            return eventLogSteps;
        }
    }
}
