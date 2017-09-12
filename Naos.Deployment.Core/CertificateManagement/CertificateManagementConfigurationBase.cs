// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateManagementConfigurationBase.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.ComponentModel;

    using Naos.Deployment.Persistence;

    /// <summary>
    /// Class to hold necessary information to create a certificate retriever.
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public abstract class CertificateManagementConfigurationBase
    {
    }

    /// <summary>
    /// Database implementation of <see cref="CertificateManagementConfigurationBase"/>.
    /// </summary>
    public class CertificateManagementConfigurationDatabase : CertificateManagementConfigurationBase
    {
        /// <summary>
        /// Gets or sets the database connection that the certificate retriever is stored in.
        /// </summary>
        public DeploymentDatabase Database { get; set; }
    }

    /// <summary>
    /// File based implementation of <see cref="CertificateManagementConfigurationBase"/>.
    /// </summary>
    public class CertificateManagementConfigurationFile : CertificateManagementConfigurationBase
    {
        /// <summary>
        /// Gets or sets the file that the certificate retriever is stored in.
        /// </summary>
        public string FilePath { get; set; }
    }
}