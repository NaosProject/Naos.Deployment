// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DatabaseRestoreBase.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System.ComponentModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base class to describe a restore of a database.
    /// </summary>
    [KnownType(typeof(DatabaseRestoreFromS3))]
    [Bindable(BindableSupport.Default)]
    public abstract class DatabaseRestoreBase
    {
    }

    /// <summary>
    /// Implementation of a database restore from an S3 bucket.
    /// </summary>
    public class DatabaseRestoreFromS3 : DatabaseRestoreBase
    {
        /// <summary>
        /// Gets or sets the name of the bucket to find the file in.
        /// </summary>
        public string BucketName { get; set; }

        /// <summary>
        /// Gets or sets the file name in the specified bucket to use.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the region the bucket is located in.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the access key needed to authenticate.
        /// </summary>
        public string DownloadAccessKey { get; set; }

        /// <summary>
        /// Gets or sets the secret key needed to authenticate.
        /// </summary>
        public string DownloadSecretKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to run a checksum on the restore.
        /// </summary>
        public bool RunChecksum { get; set; }
    }
}