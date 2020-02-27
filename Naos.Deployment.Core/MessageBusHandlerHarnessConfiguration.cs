// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageBusHandlerHarnessConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;

    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Object to hold settings related to the packages used for the harness on a <see cref="InitializationStrategyMessageBusHandler" /> installation.
    /// </summary>
    public class MessageBusHandlerHarnessConfiguration
    {
        /// <summary>
        /// Gets or sets the package (with initialization strategies and necessary overrides) to be deployed as the harness.
        /// </summary>
        public PackageDescriptionWithOverrides Package { get; set; }

        /// <summary>
        /// Gets or sets the time to allow the handler harness process to run before recycling.
        /// </summary>
        public TimeSpan HandlerHarnessProcessTimeToLive { get; set; }

        /// <summary>
        /// Gets or sets the log processor settings to be used when deploying the harness.
        /// </summary>
        public LogWritingSettings LogWritingSettings { get; set; }

        /// <summary>
        /// Gets or sets connection configuration for the message bus handler harness to be configured with.
        /// </summary>
        public MessageBusConnectionConfiguration PersistenceConnectionConfiguration { get; set; }
    }

    /// <summary>
    /// Object to hold settings related to the packages used for management of a <see cref="InitializationStrategySqlServer" /> or <see cref="InitializationStrategyMongo" /> installations.
    /// </summary>
    public class DatabaseManagementConfiguration
    {
        /// <summary>
        /// Gets or sets the package (with initialization strategies and necessary overrides) to be deployed as the harness.
        /// </summary>
        public PackageDescriptionWithOverrides FileSystemManagementPackage { get; set; }

        /// <summary>
        /// Gets or sets the package (with initialization strategies and necessary overrides) to be deployed as the harness.
        /// </summary>
        public PackageDescriptionWithOverrides DatabaseManagementPackage { get; set; }

        /// <summary>
        /// Gets or sets the log processor settings to be used when deploying the harness.
        /// </summary>
        public LogWritingSettings FileSystemManagementLogWritingSettings { get; set; }

        /// <summary>
        /// Gets or sets the log processor settings to be used when deploying the harness.
        /// </summary>
        public LogWritingSettings DatabaseManagementLogWritingSettings { get; set; }

        /// <summary>
        /// Gets or sets the time to allow the handler harness process to run before recycling.
        /// </summary>
        public TimeSpan HandlerHarnessProcessTimeToLive { get; set; }

        /// <summary>
        /// Gets or sets connection configuration for the message bus handler harness to be configured with.
        /// </summary>
        public MessageBusConnectionConfiguration PersistenceConnectionConfiguration { get; set; }
    }
}
