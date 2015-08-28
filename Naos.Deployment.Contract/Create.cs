// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Create.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Class to consolidate database settings.
    /// </summary>
    public class Create
    {
        // split apart file size and name settings because of the way defaults are generated internally.

        /// <summary>
        /// Gets or sets the file name settings.
        /// </summary>
        public DatabaseFileNameSettings DatabaseFileNameSettings { get; set; }

        /// <summary>
        /// Gets or sets the file size settings.
        /// </summary>
        public DatabaseFileSizeSettings DatabaseFileSizeSettings { get; set; }
    }

    /// <summary>
    /// Class to hold database file name settings.
    /// </summary>
    public class DatabaseFileNameSettings
    {
        /// <summary>
        /// Gets or sets the logical name of the data file.
        /// </summary>
        public string DataFileLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the name of the data file on disk.
        /// </summary>
        public string DataFileNameOnDisk { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the log file.
        /// </summary>
        public string LogFileLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the name of the log file on disk.
        /// </summary>
        public string LogFileNameOnDisk { get; set; }
    }

    /// <summary>
    /// Settings class to hold settings for creating databases.
    /// </summary>
    public class DatabaseFileSizeSettings
    {
        /// <summary>
        /// Gets or sets the current size of the data file in kilobytes.
        /// </summary>
        public long DataFileCurrentSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the max size of the data file in kilobytes.
        /// </summary>
        public long DataFileMaxSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the growth size (amount to grow when running low) of the data file in kilobytes.
        /// </summary>
        public long DataFileGrowthSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the current size of the log file in kilobytes.
        /// </summary>
        public long LogFileCurrentSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the max size of the log file in kilobytes.
        /// </summary>
        public long LogFileMaxSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the growth size (amount to grow when running low) of the log file in kilobytes.
        /// </summary>
        public long LogFileGrowthSizeInKb { get; set; }
    }
}