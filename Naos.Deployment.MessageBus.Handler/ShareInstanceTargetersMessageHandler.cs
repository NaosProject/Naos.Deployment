// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareInstanceTargetersMessageHandler.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;

    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Scheduler;
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Handler for start instance messages.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeters", Justification = "Spelling/name is correct.")]
    public class ShareInstanceTargetersMessageHandler : MessageHandlerBase<ShareInstanceTargeterMessage>, IShareInstanceTargeters
    {
        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <inheritdoc cref="MessageHandlerBase{T}" />
        public override async Task HandleAsync(ShareInstanceTargeterMessage message)
        {
            Log.Write(() => new { Info = "Sharing Targeter", MessageJson = LoggingHelper.SerializeToString(message) });
            this.InstanceTargeters = await Task.FromResult(message.InstanceTargetersToShare);
        }
    }
}
