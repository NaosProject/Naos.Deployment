// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyMongo.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Custom extension of the InitializationStrategyBase to accommodate creating a mongo database.
    /// </summary>
    public class InitializationStrategyMongo : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether this is a mongo strategy.
        /// </summary>
        public bool IsMongo { get; set; }

        /// <summary>
        /// Gets or sets the administrator password to use.
        /// </summary>
        public string AdministratorPassword { get; set; }

        /// <summary>
        /// Gets or sets the directory to save data files in.
        /// </summary>
        public string DataDirectory { get; set; }

        /// <summary>
        /// Gets or sets the directory to save log files in.
        /// </summary>
        public string LogDirectory { get; set; }
    }
}