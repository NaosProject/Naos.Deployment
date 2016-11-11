// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareInstanceTargetersMessageHandler.cs" company="Naos">
//   Copyright 2015 Naos
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
    public class ShareInstanceTargetersMessageHandler : IHandleMessages<ShareInstanceTargeterMessage>, IShareInstanceTargeters
    {
        /// <inheritdoc />
        public string Description { get; set; }

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
