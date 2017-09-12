// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Create.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;

    /// <summary>
    /// Class to consolidate database settings.
    /// </summary>
    public class Create : ICloneable
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

        /// <inheritdoc />
        public object Clone()
        {
            var ret = new Create
                          {
                              DatabaseFileNameSettings =
                                  (DatabaseFileNameSettings)this.DatabaseFileNameSettings.Clone(),
                              DatabaseFileSizeSettings =
                                  (DatabaseFileSizeSettings)this.DatabaseFileSizeSettings.Clone(),
                          };
            return ret;
        }
    }

    /// <summary>
    /// Class to hold database file name settings.
    /// </summary>
    public class DatabaseFileNameSettings : ICloneable
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

        /// <inheritdoc />
        public object Clone()
        {
            var ret = new DatabaseFileNameSettings
                          {
                              DataFileLogicalName = this.DataFileLogicalName,
                              DataFileNameOnDisk = this.DataFileNameOnDisk,
                              LogFileLogicalName = this.LogFileLogicalName,
                              LogFileNameOnDisk = this.LogFileNameOnDisk,
                          };
            return ret;
        }
    }

    /// <summary>
    /// Settings class to hold settings for creating databases.
    /// </summary>
    public class DatabaseFileSizeSettings : ICloneable
    {
        /// <summary>
        /// Gets or sets the current size of the data file in kilobytes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        public long DataFileCurrentSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the max size of the data file in kilobytes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        public long DataFileMaxSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the growth size (amount to grow when running low) of the data file in kilobytes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        public long DataFileGrowthSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the current size of the log file in kilobytes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        public long LogFileCurrentSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the max size of the log file in kilobytes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        public long LogFileMaxSizeInKb { get; set; }

        /// <summary>
        /// Gets or sets the growth size (amount to grow when running low) of the log file in kilobytes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Kb", Justification = "Spelling/name is correct.")]
        public long LogFileGrowthSizeInKb { get; set; }

        /// <inheritdoc />
        public object Clone()
        {
            var ret = new DatabaseFileSizeSettings
                          {
                              DataFileCurrentSizeInKb = this.DataFileCurrentSizeInKb,
                              DataFileGrowthSizeInKb = this.DataFileGrowthSizeInKb,
                              DataFileMaxSizeInKb = this.DataFileMaxSizeInKb,
                              LogFileCurrentSizeInKb = this.LogFileCurrentSizeInKb,
                              LogFileGrowthSizeInKb = this.LogFileGrowthSizeInKb,
                              LogFileMaxSizeInKb = this.LogFileMaxSizeInKb,
                          };

            return ret;
        }
    }
}