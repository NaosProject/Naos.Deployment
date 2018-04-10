// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyOnetimeCall.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Custom extension of the DeploymentConfiguration to accommodate console applications run by scheduled tasks deployments.
    /// </summary>
    public class InitializationStrategyOnetimeCall : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the name of the executable to run.
        /// </summary>
        public string ExeFilePathRelativeToPackageRoot { get; set; }

        /// <summary>
        /// Gets or sets the arguments to pass to the executable.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Gets or sets the justification for running a call that is not scheduled.
        /// </summary>
        public string JustificationForOnetimeCall { get; set; }

        /// <summary>
        /// Gets or sets the slot to execute in.
        /// </summary>
        public SetupStepExecutionSlot SetupStepExecutionSlot { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new InitializationStrategyOnetimeCall
            {
                              ExeFilePathRelativeToPackageRoot = this.ExeFilePathRelativeToPackageRoot,
                              Arguments = this.Arguments,
                              JustificationForOnetimeCall = this.JustificationForOnetimeCall,
                          };
            return ret;
        }
    }
}
