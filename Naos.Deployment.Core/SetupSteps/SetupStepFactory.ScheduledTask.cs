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

    using Naos.Cron;
    using Naos.Deployment.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private List<SetupStep> GetScheduledTaskSpecificSteps(InitializationStrategyScheduledTask scheduledTaskStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string consoleRootPath, string environment, string adminPassword)
        {
            var schedule = scheduledTaskStrategy.Schedule;
            var exeName = scheduledTaskStrategy.ExeName;
            var name = scheduledTaskStrategy.Name;
            var description = scheduledTaskStrategy.Description;
            var arguments = scheduledTaskStrategy.Arguments;
            var scheduledTaskAccount = this.GetAccountToUse(scheduledTaskStrategy);

            return this.GetScheduledTaskSpecificStepsParameterizedWithoutStrategy(itsConfigOverrides, consoleRootPath, environment, exeName, schedule, scheduledTaskAccount, adminPassword, name, description, arguments);
        }

        // No specific strategy is used in params so the logic can be shared.
        private List<SetupStep> GetScheduledTaskSpecificStepsParameterizedWithoutStrategy(ICollection<ItsConfigOverride> itsConfigOverrides, string consoleRootPath, string environment, string exeName, ScheduleBase schedule, string scheduledTaskAccount, string adminPassword, string name, string description, string arguments)
        {
            var scheduledTaskSetupSteps = new List<SetupStep>();

            var exeFullPath = Path.Combine(consoleRootPath, exeName);
            var exeConfigFullPath = exeFullPath + ".config";

            scheduledTaskSetupSteps.Add(
                new SetupStep
                    {
                        Description = "Enable history for scheduled tasks",
                        SetupFunc = machineManager => machineManager.RunScript(this.settings.DeploymentScriptBlocks.EnableScheduledTaskHistory.ScriptText)
                    });
            
            var itsConfigSteps = this.GetItsConfigSteps(itsConfigOverrides, consoleRootPath, environment, exeConfigFullPath);
            scheduledTaskSetupSteps.AddRange(itsConfigSteps);

            // in case we're serializing an expression schedule run the loop into a new object...
            var cronExpression = ScheduleCronExpressionConverter.ToCronExpression(schedule);
            var scheduleObject = ScheduleCronExpressionConverter.FromCronExpression(cronExpression);

            TimeSpan repetitionInterval;
            DateTime dateTimeInUtc;
            var daysOfWeek = new List<DayOfWeek>();
            var utcNow = DateTime.UtcNow;
            if (scheduleObject.GetType() == typeof(IntervalSchedule))
            {
                var intervalSchedule = (IntervalSchedule)scheduleObject;
                dateTimeInUtc = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
                repetitionInterval = intervalSchedule.Interval;
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
                throw new NotSupportedException("Unsupported schedule type for scheduled task deployment: " + scheduleObject.GetType());
            }

            var scheduledTaskPassword = scheduledTaskAccount == null
                                            ? null
                                            : scheduledTaskAccount.ToUpperInvariant() == this.AdministratorAccount.ToUpperInvariant() ? adminPassword : null;

            var setupScheduledTaskParams = new object[] { name, description, scheduledTaskAccount, scheduledTaskPassword, exeFullPath, arguments, dateTimeInUtc, repetitionInterval, daysOfWeek.ToArray() };
            var createScheduledTask = new SetupStep
                                          {
                                              Description =
                                                  "Creating scheduled task to run: " + exeName + " " + (arguments ?? "<no arguments>") + " with schedule: "
                                                  + cronExpression,
                                              SetupFunc =
                                                  machineManager =>
                                                  machineManager.RunScript(
                                                      this.settings.DeploymentScriptBlocks.SetupScheduledTask.ScriptText,
                                                      setupScheduledTaskParams)
                                          };

            scheduledTaskSetupSteps.Add(createScheduledTask);

            return scheduledTaskSetupSteps;
        }

        private string GetAccountToUse(InitializationStrategyScheduledTask scheduledTaskStrategy)
        {
            var scheduledTaskAccount = string.IsNullOrEmpty(scheduledTaskStrategy.ScheduledTaskAccount)
                                           ? this.settings.HarnessSettings.HarnessAccount
                                           : scheduledTaskStrategy.ScheduledTaskAccount;
            return scheduledTaskAccount;
        }
    }
}
