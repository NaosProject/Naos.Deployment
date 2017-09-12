// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareInstanceTargetersMessageHandler.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;

    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Contract;
    using Naos.MessageBus.Domain;

    /// <summary>
    /// Handler for start instance messages.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeters", Justification = "Spelling/name is correct.")]
    public class ShareInstanceTargetersMessageHandler : IHandleMessages<ShareInstanceTargeterMessage>, IShareInstanceTargeters
    {
        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(ShareInstanceTargeterMessage message)
        {
            Log.Write(() => new { Info = "Sharing Targeter", MessageJson = message.ToJson() });
            this.InstanceTargeters = await Task.FromResult(message.InstanceTargetersToShare);
        }
    }
}
