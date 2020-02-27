// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DatabaseRestoreBase.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Base class to describe a restore of a database.
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public abstract class DatabaseRestoreBase : ICloneable
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
        public abstract object Clone();
    }

    /// <summary>
    /// Null object implementation for testing.
    /// </summary>
    public class NullDatabaseRestore : DatabaseRestoreBase
    {
        /// <inheritdoc />
        public override object Clone()
        {
            return new NullDatabaseRestore();
        }
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

        /// <inheritdoc />
        public override object Clone()
        {
            var ret = new DatabaseRestoreFromS3
                          {
                              BucketName = this.BucketName,
                              FileName = this.FileName,
                              Region = this.Region,
                              DownloadAccessKey = this.DownloadAccessKey,
                              DownloadSecretKey = this.DownloadSecretKey,
                              RunChecksum = this.RunChecksum,
                          };
            return ret;
        }
    }
}
