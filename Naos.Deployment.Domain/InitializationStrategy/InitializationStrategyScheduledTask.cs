// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyScheduledTask.cs" company="Naos">
//   Copyright 2015 Naos
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
        public string ExeName { get; set; }

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

        /// <inheritdoc />
        public override object Clone()
        {
            var schedule = (ScheduleBase)this.Schedule.Clone();
            var ret = new InitializationStrategyScheduledTask
                          {
                              Name = this.Name,
                              Description = this.Description,
                              ExeName = this.ExeName,
                              Arguments = this.Arguments,
                              Schedule = schedule
                          };
            return ret;
        }
    }
}
