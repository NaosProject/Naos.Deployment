// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AutoStartProvider.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Model object to describe an Auto Start Provider in IIS
    /// </summary>
    public class AutoStartProvider
    {
        /// <summary>
        /// Gets or sets the name of the auto start provider.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the auto start provider (i.e. "MyNamespace.MyAutoStartProviderClass, MyAssembly").
        /// </summary>
        public string Type { get; set; }
    }
}