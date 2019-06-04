// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HandlerFactory.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Hangfire.Console
{
    using System;
    using System.Collections.Generic;

    using Naos.Deployment.MessageBus.Handler;
    using Naos.Deployment.MessageBus.Scheduler;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Factory builder to provide logic to resolve the appropriate <see cref="IHandleMessages" /> for a dispatched <see cref="IMessage" /> implementation.
    /// </summary>
    public static partial class HandlerFactory
    {
        /*----------------------------- CHANGE HERE ---------------------------------*
         * Can specify the map directly or instead use the example function below to *
         * discover your handlers in 1 or many assemblies.                           *
         *---------------------------------------------------------------------------*/

        /// <summary>
        /// Map of the message type to the intended handler type.  Must have a parameterless constructor and implement <see cref="IHandleMessages" />,
        /// however deriving from <see cref="MessageHandlerBase{T}" /> is recommended as it's more straightforward and easier to write.
        /// </summary>
        private static readonly IReadOnlyDictionary<Type, Type> MessageTypeToHandlerTypeMap = DiscoverHandlersInAssemblies(new[] { typeof(StartInstanceMessage).Assembly, typeof(StartInstanceMessageHandler).Assembly });
    }
}