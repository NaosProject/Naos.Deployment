// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChangeInstanceTypeMessageHandler.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.MessageBus.Handler
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Its.Configuration;
    using Its.Log.Instrumentation;

    using Naos.Deployment.ComputingManagement;
    using Naos.Deployment.Domain;
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
        public InstanceTargeterBase[] InstanceTargeters { get; set; }

        /// <inheritdoc />
        public async Task HandleAsync(ChangeInstanceTypeMessage message)
        {
            var settings = Settings.Get<DeploymentMessageHandlerSettings>();
            var computingInfrastructureManagerSettings = Settings.Get<ComputingInfrastructureManagerSettings>();
            await this.Handle(message, settings, computingInfrastructureManagerSettings);
        }

        /// <summary>
        /// Handle a change instance type message.
        /// </summary>
        /// <param name="message">Message to handle.</param>
        /// <param name="settings">Settings necessary to handle the message.</param>
        /// <param name="computingInfrastructureManagerSettings">Settings for the cloud infrastructure manager.</param>
        /// <returns>Task for async execution.</returns>
        public async Task Handle(ChangeInstanceTypeMessage message, DeploymentMessageHandlerSettings settings, ComputingInfrastructureManagerSettings computingInfrastructureManagerSettings)
        {
            if (message.NewInstanceType == null)
            {
                throw new ArgumentException("Must specify a new instance type to change instance to.");
            }

            if (message == null)
            {
                throw new ArgumentException("Cannot have a null message.");
            }

            if (message.InstanceTargeters == null || message.InstanceTargeters.Length == 0)
            {
                throw new ArgumentException("Must specify at least one instance targeter to use for specifying an instance.");
            }

            var cloudManager = ComputingManagerHelper.CreateComputingManager(settings, computingInfrastructureManagerSettings);

            var tasks =
                message.InstanceTargeters.Select(
                    instanceTargeter =>
                    Task.Run(
                        () => OperationToParallelize(instanceTargeter, settings, cloudManager, message.NewInstanceType)))
                    .ToArray();

            await Task.WhenAll(tasks);

            this.InstanceTargeters = message.InstanceTargeters;
        }

        private static async Task OperationToParallelize(
            InstanceTargeterBase instanceTargeter,
            DeploymentMessageHandlerSettings settings,
            IManageComputingInfrastructure computingManager,
            InstanceType newInstanceType)
        {
            var systemId =
                await ComputingManagerHelper.GetSystemIdFromTargeterAsync(instanceTargeter, settings, computingManager);

            Log.Write(
                () =>
                new
                    {
                        Info = "Changing Instance Type",
                        InstanceTargeterJson = Serializer.Serialize(instanceTargeter),
                        NewInstanceType = Serializer.Serialize(newInstanceType),
                        SystemId = systemId
                    });

            await computingManager.ChangeInstanceTypeAsync(systemId, settings.SystemLocation, newInstanceType);
        }
    }
}