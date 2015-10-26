// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentSettings.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using Naos.MessageBus.HandlingContract;

    /// <summary>
    /// Object with supporting settings to launch a deployment manager.
    /// </summary>
    public class DeploymentSettings
    {
        /// <summary>
        /// Gets or sets the default deployment configuration to be used when none is found in the package and no override is specified.
        /// </summary>
        public DeploymentConfiguration DefaultDeploymentConfig { get; set; }

        /// <summary>
        /// Gets or sets the settings related to the harness to deploy for message bus handler packages.
        /// </summary>
        public MessageBusHandlerHarnessSettings MessageBusHandlerHarnessSettings { get; set; }
    }

    /// <summary>
    /// Object to hold settings related to the harness to deploy for message bus handler packages.
    /// </summary>
    public class MessageBusHandlerHarnessSettings
    {
        /// <summary>
        /// Gets or sets the package (with initialization strategies and necessary overrides) to be deployed as the harness.
        /// </summary>
        public PackageDescriptionWithOverrides Package { get; set; }

        /// <summary>
        /// Gets or sets the log processor settings to be used when deploying the harness.
        /// </summary>
        public LogProcessorSettings LogProcessorSettings { get; set; }
    }
}
