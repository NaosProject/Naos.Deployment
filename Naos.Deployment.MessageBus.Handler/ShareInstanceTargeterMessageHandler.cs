// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShareInstanceTargeterMessageHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System.Threading.Tasks;

    using Its.Log.Instrumentation;

    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;
    using Naos.Deployment.MessageBus.Contract;
    using Naos.MessageBus.HandlingContract;

    /// <summary>
    /// Handler for start instance messages.
    /// </summary>
    public class ShareInstanceTargeterMessageHandler : IHandleMessages<ShareInstanceTargeterMessage>, IShareInstanceTargeter
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(ShareInstanceTargeterMessage message)
        {
            Log.Write(() => new { Info = "Sharing Targeter", MessageJson = Serializer.Serialize(message) });
            this.InstanceTargeters = await Task.FromResult(message.InstanceTargetersToShare);
        }
    }
}
