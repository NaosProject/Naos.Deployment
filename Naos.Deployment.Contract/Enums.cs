// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Enums.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    /// <summary>
    /// Enumeration of the type of access an instance needs.
    /// </summary>
    public enum InstanceAccessibility
    {
        /// <summary>
        /// Indicates that it's irrelevant to this deployment.
        /// </summary>
        DoesntMatter,

        /// <summary>
        /// Indicates accessible to the public internet.
        /// </summary>
        Public,

        /// <summary>
        /// Indicates accessible only inside the private network.
        /// </summary>
        Private
    }
}
