// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InjectedPackage.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    /// <summary>
    /// Model object to track an injected package into a deployment.
    /// </summary>
    public class InjectedPackage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InjectedPackage"/> class.
        /// </summary>
        /// <param name="reason">Reason for injection.</param>
        /// <param name="packagedConfig">Packaged config to add.</param>
        public InjectedPackage(string reason, PackagedDeploymentConfiguration packagedConfig)
        {
            this.Reason = reason;
            this.PackagedConfig = packagedConfig;
        }

        /// <summary>
        /// Gets the reason for adding the package.
        /// </summary>
        public string Reason { get; private set; }

        /// <summary>
        /// Gets the packaged config to add to deployment.
        /// </summary>
        public PackagedDeploymentConfiguration PackagedConfig { get; private set; }
    }
}
