// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CertificateRetrieverConfigurationBase.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System.ComponentModel;

    using Naos.Deployment.Persistence;

    /// <summary>
    /// Class to hold necessary information to create a certificate retriever.
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public abstract class CertificateRetrieverConfigurationBase
    {
    }

    /// <summary>
    /// Database implementation of <see cref="CertificateRetrieverConfigurationBase"/>.
    /// </summary>
    public class CertificateRetrieverConfigurationDatabase : CertificateRetrieverConfigurationBase
    {
        /// <summary>
        /// Gets or sets the database connection that the certificate retriever is stored in.
        /// </summary>
        public DeploymentDatabase Database { get; set; }
    }

    /// <summary>
    /// File based implementation of <see cref="CertificateRetrieverConfigurationBase"/>.
    /// </summary>
    public class CertificateRetrieverConfigurationFile : CertificateRetrieverConfigurationBase
    {
        /// <summary>
        /// Gets or sets the file that the certificate retriever is stored in.
        /// </summary>
        public string FilePath { get; set; }
    }
}