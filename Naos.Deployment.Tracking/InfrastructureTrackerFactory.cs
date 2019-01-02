// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InfrastructureTrackerFactory.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Tracking
{
    using System;

    using MongoDB.Bson;

    using Naos.Deployment.Domain;
    using Naos.Deployment.Persistence;

    using static System.FormattableString;

    /// <summary>
    /// Factory for creating infrastructure trackers.
    /// </summary>
    public static class InfrastructureTrackerFactory
    {
        /// <summary>
        /// Creates an infrastructure tracker from configuration provided.
        /// </summary>
        /// <param name="infrastructureTrackerConfigurationBase">Configuration to use when creating an infrastructure tracker.</param>
        /// <returns>An implementation of the <see cref="ITrackComputingInfrastructure"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Prefer this layout.")]
        public static ITrackComputingInfrastructure Create(InfrastructureTrackerConfigurationBase infrastructureTrackerConfigurationBase)
        {
            ITrackComputingInfrastructure ret;

            if (infrastructureTrackerConfigurationBase is InfrastructureTrackerConfigurationFolder)
            {
                var configAsFolder = (InfrastructureTrackerConfigurationFolder)infrastructureTrackerConfigurationBase;
                ret = new RootFolderEnvironmentFolderInstanceFileTracker(configAsFolder.RootFolderPath);
            }
            else if (infrastructureTrackerConfigurationBase is InfrastructureTrackerConfigurationDatabase)
            {
                var configAsDatabase = (InfrastructureTrackerConfigurationDatabase)infrastructureTrackerConfigurationBase;
                var deploymentDatabase = configAsDatabase.Database;
                var arcologyInfoQueries = deploymentDatabase.GetQueriesInterface<ArcologyInfoContainer>();
                var arcologyInfoCommands = deploymentDatabase.GetCommandsInterface<string, ArcologyInfoContainer>();
                var instanceQueries = deploymentDatabase.GetQueriesInterface<InstanceContainer>();
                var instanceCommands = deploymentDatabase.GetCommandsInterface<string, InstanceContainer>();
                ret = new MongoInfrastructureTracker(arcologyInfoQueries, arcologyInfoCommands, instanceQueries, instanceCommands);
            }
            else if (infrastructureTrackerConfigurationBase is InfrastructureTrackerConfigurationNull)
            {
                ret = new NullInfrastructureTracker();
            }
            else
            {
                throw new NotSupportedException(Invariant($"Configuration is not valid: {infrastructureTrackerConfigurationBase.ToJson()}"));
            }

            return ret;
        }
    }
}
