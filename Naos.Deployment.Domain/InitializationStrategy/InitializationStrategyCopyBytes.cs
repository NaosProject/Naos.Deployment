// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyCopyBytes.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Custom extension of the InitializationStrategyBase to accomodate copying bytes only for use by some other process (generally dangerous as it hides dependencies).
    /// </summary>
    public class InitializationStrategyCopyBytes : InitializationStrategyBase
    {
        /// <summary>
        /// Gets or sets the justification for copying the package bytes to the server when it's not required by other strategies.
        /// </summary>
        public string JustificationForCopyPackage { get; set; }

        /// <inheritdoc />
        public override object Clone()
        {
            return new InitializationStrategyCopyBytes { JustificationForCopyPackage = this.JustificationForCopyPackage };
        }
    }
}
