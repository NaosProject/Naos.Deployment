// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChangeInstanceTypeMessageHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Its.Configuration;
    using Its.Log.Instrumentation;

    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;
    using Naos.Deployment.MessageBus.Contract;
    using Naos.MessageBus.HandlingContract;

    /// <summary>
    /// Handler for stop instance messages.
    /// </summary>
    public class ChangeInstanceTypeMessageHandler : IHandleMessages<ChangeInstanceTypeMessage>, IShareInstanceTargeter
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public IList<InstanceTargeterBase> InstanceTargeters { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(ChangeInstanceTypeMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var cloudInfrastructureManagerSettings = Settings.Get<CloudInfrastructureManagerSettings>();
            await this.Handle(message, settings, cloudInfrastructureManagerSettings);
        }

        /// <summary>
        /// Handle a change instance type message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="cloudInfrastructureManagerSettings">Settings for the cloud infrastructure manager.</param>
        /// <returns>Task for async execution.</returns>
        public async Task Handle(ChangeInstanceTypeMessage message, DeploymentMessageHandlerSettings settings, CloudInfrastructureManagerSettings cloudInfrastructureManagerSettings)
        {
            if (message.NewInstanceType == null)
            {
                throw new ArgumentException("Must specify a new instance type to change instance to.");
            }

            if (message == null)
            {
                throw new ArgumentException("Cannot have a null message.");
            }

            if (message.InstanceTargeters == null || message.InstanceTargeters.Count == 0)
            {
                throw new ArgumentException("Must specify at least one instance targeter to use for specifying an instance.");
            }

            var cloudManager = CloudManagerHelper.CreateCloudManager(settings, cloudInfrastructureManagerSettings);

            var tasks =
                message.InstanceTargeters.Select(
                    (instanceTargeter) =>
                    Task.Run(
                        () => OperationToParallelize(instanceTargeter, settings, cloudManager, message.NewInstanceType)))
                    .ToArray();

            await Task.WhenAll(tasks);

            this.InstanceTargeters = message.InstanceTargeters;
        }

        private static async Task OperationToParallelize(
            InstanceTargeterBase instanceTargeter,
            DeploymentMessageHandlerSettings settings,
            IManageCloudInfrastructure cloudManager,
            InstanceType newInstanceType)
        {
            var systemId =
                await CloudManagerHelper.GetSystemIdFromTargeterAsync(instanceTargeter, settings, cloudManager);

            Log.Write(
                () =>
                new
                    {
                        Info = "Changing Instance Type",
                        InstanceTargeterJson = Serializer.Serialize(instanceTargeter),
                        NewInstanceType = Serializer.Serialize(newInstanceType),
                        SystemId = systemId
                    });

            await cloudManager.ChangeInstanceTypeAsync(systemId, settings.SystemLocation, newInstanceType);
        }
    }
}