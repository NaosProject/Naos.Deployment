﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitializationStrategyBase.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Strategy to initialize the application.
    /// </summary>
    [Bindable(BindableSupport.Default)]
    public abstract class InitializationStrategyBase : ICloneable
    {
        /// <summary>
        /// Clone method to duplicate the strategy in a way that can be used without damaging the original copy.
        /// </summary>
        /// <returns>A deeply clone duplicate of the object.</returns>
        public abstract object Clone();
    }
}
