// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IManageCloudInfrastructure.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Interface for performing native cloud operations.
    /// </summary>
    public interface IManageCloudInfrastructure
    {
        /// <summary>
        /// Terminates an instance.
        /// </summary>
        /// <param name="systemId">Proprietary ID of the instance.</param>
        /// <param name="systemLocation">Proprietary location of the instance.</param>
        void Terminate(string systemId, string systemLocation);

        /// <summary>
        /// Creates a new instance per the deployment configuration provided.
        /// </summary>
        /// <param name="name">Name of the instance.</param>
        /// <param name="deploymentConfiguration">Deployment configuration to use to build a new instance.</param>
        /// <returns>Description of created instance.</returns>
        InstanceDescription Create(string name, DeploymentConfiguration deploymentConfiguration);

        /// <summary>
        /// Gets the administrator password for the specified instance.
        /// </summary>
        /// <param name="instanceDescription">Description of the instance in question.</param>
        /// <param name="privateKey">Decryption key needed for password.</param>
        /// <returns>Password of the instance's administrator account.</returns>
        string GetAdministratorPasswordForInstance(InstanceDescription instanceDescription, string privateKey);
    }
}
