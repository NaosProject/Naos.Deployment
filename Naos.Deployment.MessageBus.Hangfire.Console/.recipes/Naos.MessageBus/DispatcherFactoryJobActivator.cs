﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DispatcherFactoryJobActivator.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// <auto-generated>
//   Sourced from NuGet package. Will be overwritten with package update except in Naos.MessageBus.Hangfire.Bootstrapper source.
// </auto-generated>
// --------------------------------------------------------------------------------------------------------------------

#if NaosMessageBusHangfireConsole
namespace Naos.MessageBus.Hangfire.Console
#else
namespace Naos.MessageBus.Hangfire.Bootstrapper
#endif
{
    using System;

    using global::Hangfire;

    using Naos.MessageBus.Core;
    using Naos.MessageBus.Domain;
    using Naos.MessageBus.Domain.Exceptions;
    using Naos.MessageBus.Hangfire.Sender;

    using OBeautifulCode.TypeRepresentation;

    using Spritely.Recipes;

    using static System.FormattableString;

#pragma warning disable CS3009 // Base type is not CLS-compliant - need for Hangfire contract.
    /// <summary>
    /// Hangfire job activator that will dispatch the job to the <see cref="MessageDispatcher" />.
    /// </summary>
#if !NaosMessageBusHangfireConsole
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [System.CodeDom.Compiler.GeneratedCode("Naos.MessageBus.Hangfire.Bootstrapper", "See package version number")]
#endif
    public class DispatcherFactoryJobActivator : JobActivator
#pragma warning restore CS3009 // Base type is not CLS-compliant
    {
        // Make this permissive since it's the underlying logic and shouldn't be coupled to whether handlers are matched in strict mode...
        private readonly TypeComparer typeComparer = new TypeComparer(TypeMatchStrategy.NamespaceAndName);

        private readonly IDispatchMessages messageDispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherFactoryJobActivator"/> class.
        /// </summary>
        /// <param name="messageDispatcher">Dispatcher manager to .</param>
        public DispatcherFactoryJobActivator(IDispatchMessages messageDispatcher)
        {
            this.messageDispatcher = messageDispatcher ?? throw new ArgumentNullException(nameof(messageDispatcher));
        }

        /// <inheritdoc />
        public override object ActivateJob(Type jobType)
        {
            new { jobType }.Must().NotBeNull().OrThrowFirstFailure();

            if (this.typeComparer.Equals(jobType, typeof(HangfireDispatcher)))
            {
                return new HangfireDispatcher(this.messageDispatcher);
            }

            throw new DispatchException(Invariant($"Attempted to load type other than {nameof(IDispatchMessages)}, type: {jobType.FullName}"));
        }
    }
}