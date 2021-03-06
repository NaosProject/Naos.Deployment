﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategySelfHost.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Custom extension of the DeploymentConfiguration to accommodate self host deployments.
    /// </summary>
    public class InitializationStrategySelfHost : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the path of the executable in the package that is hosting.
        /// </summary>
        public string SelfHostExeFilePathRelativeToPackageRoot { get; set; }

        /// <summary>
        /// Gets or sets the arguments to pass to the executable.
        /// </summary>
        public string SelfHostArguments { get; set; }

        /// <summary>
        /// Gets or sets the DNS entries to support access of the self hosted deployment.
        /// </summary>
        public IReadOnlyCollection<string> SelfHostSupportedDnsEntries { get; set; }

        /// <summary>
        /// Gets or sets the name of the SSL certificate to lookup and use.
        /// </summary>
        public string SslCertificateName { get; set; }

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
            var ret = new InitializationStrategySelfHost
                          {
                              SelfHostExeFilePathRelativeToPackageRoot = this.SelfHostExeFilePathRelativeToPackageRoot,
                              SelfHostArguments = this.SelfHostArguments,
                              SslCertificateName = this.SslCertificateName,
                              SelfHostSupportedDnsEntries =
                                  this.SelfHostSupportedDnsEntries.Select(_ => _.Clone().ToString()).ToList(),
                              ScheduledTaskAccount = this.ScheduledTaskAccount,
                              Priority = this.Priority,
                          };

            return ret;
        }
    }
}
