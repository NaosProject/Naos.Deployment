// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.ScheduledTask.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Naos.Cron;
    using Naos.Deployment.Contract;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private List<SetupStep> GetScheduledTaskSpecificSteps(InitializationStrategyScheduledTask scheduledTaskStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string consoleRootPath, string environment)
        {
            var scheduledTaskSetupSteps = new List<SetupStep>();

            var exeFullPath = Path.Combine(consoleRootPath, scheduledTaskStrategy.ExeName);
            var exeConfigFullPath = exeFullPath + ".config";
            var updateExeConfigScriptBlock = this.settings.DeploymentScriptBlocks.UpdateItsConfigPrecedence;
            var precedenceChain = new[] { environment }.ToList();
            precedenceChain.AddRange(this.itsConfigPrecedenceAfterEnvironment);
            var updateExeConfigScriptParams = new object[] { exeConfigFullPath, precedenceChain.ToArray() };

            scheduledTaskSetupSteps.Add(
                new SetupStep
                {
                    Description = "Enable history for scheduled tasks",
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(
                            this.settings.DeploymentScriptBlocks.EnableScheduledTaskHistory.ScriptText)
                });

            scheduledTaskSetupSteps.Add(
                new SetupStep
                {
                    Description = "Update Its.Config precedence: " + string.Join("|", precedenceChain),
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(
                            updateExeConfigScriptBlock.ScriptText,
                            updateExeConfigScriptParams)
                });

            foreach (var itsConfigOverride in itsConfigOverrides ?? new List<ItsConfigOverride>())
            {
                var itsFileSubPath = string.Format(
                    ".config/{0}/{1}.json",
                    environment,
                    itsConfigOverride.FileNameWithoutExtension);

                var itsFilePath = Path.Combine(consoleRootPath, itsFileSubPath);
                var itsFileBytes = Encoding.UTF8.GetBytes(itsConfigOverride.FileContentsJson);

                scheduledTaskSetupSteps.Add(
                    new SetupStep
                    {
                        Description =
                            "(Over)write Its.Config file: " + itsConfigOverride.FileNameWithoutExtension,
                        SetupAction =
                            machineManager => machineManager.SendFile(itsFilePath, itsFileBytes, false, true)
                    });
            }

            // in case we're serializing an expression schedule run the loop into a new object...
            var cronExpression = ScheduleCronExpressionConverter.ToCronExpression(scheduledTaskStrategy.Schedule);
            var scheduleObject = ScheduleCronExpressionConverter.FromCronExpression(cronExpression);

            TimeSpan repetitionInterval;
            DateTime dateTimeInUtc;
            var daysOfWeek = new List<DayOfWeek>();
            var utcNow = DateTime.UtcNow;
            if (scheduleObject.GetType() == typeof(MinutelySchedule))
            {
                dateTimeInUtc = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
                repetitionInterval = TimeSpan.FromMinutes(1);
            }
            else if (scheduleObject.GetType() == typeof(HourlySchedule))
            {
                var hourlySchedule = (HourlySchedule)scheduleObject;
                dateTimeInUtc = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, hourlySchedule.Minute, 0, DateTimeKind.Utc);
                repetitionInterval = TimeSpan.FromHours(1);
            }
            else if (scheduleObject.GetType() == typeof(DailyScheduleInUtc))
            {
                var dailySchedule = (DailyScheduleInUtc)scheduleObject;
                dateTimeInUtc = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, dailySchedule.Hour, dailySchedule.Minute, 0, DateTimeKind.Utc);
                repetitionInterval = TimeSpan.FromDays(1);
            }
            else if (scheduleObject.GetType() == typeof(WeeklyScheduleInUtc))
            {
                var weeklySchedule = (WeeklyScheduleInUtc)scheduleObject;
                dateTimeInUtc = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, weeklySchedule.Hour, weeklySchedule.Minute, 0, DateTimeKind.Utc);
                repetitionInterval = TimeSpan.FromDays(7);
                daysOfWeek.AddRange(weeklySchedule.DaysOfWeek);
            }
            else
            {
                throw new NotSupportedException(
                    "Unsupported schedule type for scheduled task deployment: " + scheduleObject.GetType());
            }

            var setupScheduledTaskParams = new object[]
                                               {
                                                   scheduledTaskStrategy.Name, scheduledTaskStrategy.Description,
                                                   exeFullPath, scheduledTaskStrategy.Arguments, dateTimeInUtc,
                                                   repetitionInterval, daysOfWeek.ToArray()
                                               };
            var createScheduledTask = new SetupStep
            {
                Description =
                    "Creating scheduled task to run: " + scheduledTaskStrategy.ExeName + " " + scheduledTaskStrategy.Arguments + " with schedule: "
                    + cronExpression,
                SetupAction =
                    machineManager =>
                    machineManager.RunScript(
                        this.settings.DeploymentScriptBlocks.SetupScheduledTask
                        .ScriptText,
                        setupScheduledTaskParams)
            };

            scheduledTaskSetupSteps.Add(createScheduledTask);

            return scheduledTaskSetupSteps;
        }
    }
}
