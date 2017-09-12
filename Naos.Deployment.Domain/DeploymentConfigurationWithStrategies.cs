// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentConfigurationWithStrategies.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Deployment configuration with initialization strategies added to specify in config files.
    /// </summary>
    public class DeploymentConfigurationWithStrategies : DeploymentConfiguration, IHaveInitializationStrategies
    {
        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Keeping without constructor for now due to serialization issues.")]
        public ICollection<InitializationStrategyBase> InitializationStrategies { get; set; }
    }
}
