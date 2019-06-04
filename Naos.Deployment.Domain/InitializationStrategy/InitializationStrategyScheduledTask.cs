// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyScheduledTask.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using Naos.Cron;

    /// <summary>
    /// Custom extension of the DeploymentConfiguration to accommodate console applications run by scheduled tasks deployments.
    /// </summary>
    public class InitializationStrategyScheduledTask : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the name of the task.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the task.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the executable to run.
        /// </summary>
        public string ExeFilePathRelativeToPackageRoot { get; set; }

        /// <summary>
        /// Gets or sets the arguments to pass to the executable.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Gets or sets the schedule to run the task on.
        /// </summary>
        public ScheduleBase Schedule { get; set; }

        /// <summary>
        /// Gets or sets the account to configure the scheduled task that runs the executable.
        /// </summary>
        public string ScheduledTaskAccount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to run the executable with "Highest Priviledges" / "Elevated Mode".
        /// </summary>
        public bool RunElevated { get; set; }

        /// <summary>
        /// Gets or sets the priority of the task; default will be 5; acceptable values are 0-7 see https://docs.microsoft.com/en-us/windows/desktop/taskschd/tasksettings-priority.
        /// </summary>
        public int? Priority { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var schedule = (ScheduleBase)this.Schedule.Clone();
            var ret = new InitializationStrategyScheduledTask
                          {
                              Name = this.Name,
                              Description = this.Description,
                              ExeFilePathRelativeToPackageRoot = this.ExeFilePathRelativeToPackageRoot,
                              Arguments = this.Arguments,
                              Schedule = schedule,
                              ScheduledTaskAccount = this.ScheduledTaskAccount,
                              RunElevated = this.RunElevated,
                              Priority = this.Priority,
                          };
            return ret;
        }
    }
}
