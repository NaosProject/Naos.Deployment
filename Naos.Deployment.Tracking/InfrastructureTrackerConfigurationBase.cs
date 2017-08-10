// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InfrastructureTrackerConfigurationBase.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System.ComponentModel;

    using Naos.Deployment.Persistence;

    /// <summary>
    /// Class to hold necessary information to create an infrastructure tracker.
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public abstract class InfrastructureTrackerConfigurationBase
    {
    }

    /// <summary>
    /// Database implementation of <see cref="InfrastructureTrackerConfigurationBase"/>.
    /// </summary>
    public class InfrastructureTrackerConfigurationDatabase : InfrastructureTrackerConfigurationBase
    {
        /// <summary>
        /// Gets or sets the database connection that the computing infrastructure is tracked in.
        /// </summary>
        public DeploymentDatabase Database { get; set; }
    }

    /// <summary>
    /// Root folder implementation of <see cref="InfrastructureTrackerConfigurationBase"/>.
    /// </summary>
    public class InfrastructureTrackerConfigurationFolder : InfrastructureTrackerConfigurationBase
    {
        /// <summary>
        /// Gets or sets the file path of the root folder used to track the computing infrastructure.
        /// </summary>
        public string RootFolderPath { get; set; }
    }
}