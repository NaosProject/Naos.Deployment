// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageBusHandlerHarnessConfiguration.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;

    using Naos.Deployment.Domain;
    using Naos.Logging.Domain;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Object to hold settings related to the harness to deploy for message bus handler packages.
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
        public LogProcessorSettings LogProcessorSettings { get; set; }

        /// <summary>
        /// Gets or sets connection configuration for the message bus handler harness to be configured with.
        /// </summary>
        public MessageBusConnectionConfiguration PersistenceConnectionConfiguration { get; set; }
    }
}