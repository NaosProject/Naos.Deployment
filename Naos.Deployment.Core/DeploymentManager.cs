// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentManager.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Naos.AWS.Core;
    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;
    using Naos.MessageBus.HandlingContract;
    using Naos.Packaging.Domain;
    using Naos.WinRM;

    using Newtonsoft.Json;

    /// <inheritdoc />
    public class DeploymentManager : IManageDeployments
    {
        /// <summary>
        /// Lock object to only allow one DNS update at a time because AWSSDK does not seem to support this otherwise.
        /// </summary>
        private readonly object syncDnsManager = new object();

        /// <summary>
        /// Lock object to only allow one WinRM call to happen at a time because System.Management.Automation seems to *sometimes* not support this.
        /// </summary>
        private readonly object syncMachineManager = new object();

        private readonly ITrackComputingInfrastructure tracker;

        private readonly IManageCloudInfrastructure cloudManager;

        private readonly IGetPackages packageManager;

        private readonly DeploymentConfiguration defaultDeploymentConfiguration;

        private readonly Action<string> announce;

        private readonly SetupStepFactory setupStepFactory;

        private readonly string messageBusPersistenceConnectionString;

        private readonly ICollection<string> packageIdsToIgnoreDuringTerminationSearch;

        private readonly MessageBusHandlerHarnessConfiguration messageBusHandlerHarnessConfiguration;

        private readonly string[] itsConfigPrecedenceAfterEnvironment;

        private readonly ConcurrentBag<TelemetryEntry> telemetry = new ConcurrentBag<TelemetryEntry>();

        private readonly string telemetryFile;

        private readonly ConcurrentBag<AnnouncementEntry> announcements = new ConcurrentBag<AnnouncementEntry>();

        private readonly string announcementFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentManager"/> class.
        /// </summary>
        /// <param name="tracker">Tracker of computing infrastructure.</param>
        /// <param name="cloudManager">Manager of the cloud infrastructure (wraps custom cloud interactions).</param>
        /// <param name="packageManager">Proxy to retrieve packages.</param>
        /// <param name="certificateRetriever">Manager of certificates (get passwords and file bytes by name).</param>
        /// <param name="defaultDeploymentConfiguration">Default deployment configuration to substitute the values for any nulls.</param>
        /// <param name="messageBusHandlerHarnessConfiguration">Settings and description of the harness to use for message bus handler initializations.</param>
        /// <param name="setupStepFactorySettings">Settings for the setup step factory.</param>
        /// <param name="messageBusPersistenceConnectionString">Connection string to the message bus harness.</param>
        /// <param name="packageIdsToIgnoreDuringTerminationSearch">List of package IDs to exclude during replacement search.</param>
        /// <param name="announcer">Callback to get status messages through process.</param>
        /// <param name="announcementFile">Optional file path to record a JSON file of announcements.</param>
        /// <param name="telemetryFile">Optional file path to record JSON file of certain task timings.</param>
        public DeploymentManager(
            ITrackComputingInfrastructure tracker,
            IManageCloudInfrastructure cloudManager,
            IGetPackages packageManager,
            IGetCertificates certificateRetriever,
            DefaultDeploymentConfiguration defaultDeploymentConfiguration,
            MessageBusHandlerHarnessConfiguration messageBusHandlerHarnessConfiguration,
            SetupStepFactorySettings setupStepFactorySettings,
            string messageBusPersistenceConnectionString,
            ICollection<string> packageIdsToIgnoreDuringTerminationSearch,
            Action<string> announcer, 
            string announcementFile = null, 
            string telemetryFile = null)
        {
            this.tracker = tracker;
            this.cloudManager = cloudManager;
            this.packageManager = packageManager;
            this.defaultDeploymentConfiguration = defaultDeploymentConfiguration;
            this.messageBusPersistenceConnectionString = messageBusPersistenceConnectionString;
            this.messageBusHandlerHarnessConfiguration = messageBusHandlerHarnessConfiguration;
            this.packageIdsToIgnoreDuringTerminationSearch = packageIdsToIgnoreDuringTerminationSearch;
            this.announce = announcer;
            this.telemetryFile = telemetryFile;
            this.announcementFile = announcementFile;
            this.itsConfigPrecedenceAfterEnvironment = new[] { "Common" };
            this.setupStepFactory = new SetupStepFactory(
                setupStepFactorySettings,
                certificateRetriever,
                packageManager,
                this.itsConfigPrecedenceAfterEnvironment);
        }

        /// <inheritdoc />
        public void DeployPackages(ICollection<PackageDescriptionWithOverrides> packagesToDeploy, string environment, string instanceName, DeploymentConfiguration deploymentConfigOverride = null)
        {
            if (this.telemetryFile != null)
            {
                this.LogAnnouncement("Logging telemetry to: " + this.telemetryFile);
            }
            else
            {
                this.LogAnnouncement("Not logging telemetry to a file");
            }

            if (this.announcementFile != null)
            {
                this.LogAnnouncement("Logging announcements to: " + this.announcementFile);
            }
            else
            {
                this.LogAnnouncement("Not logging announcements to a file");
            }

            if (packagesToDeploy == null)
            {
                packagesToDeploy = new List<PackageDescriptionWithOverrides>();
            }

            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = string.Join("---", packagesToDeploy.Select(_ => _.Id.Replace(".", "-")).ToArray());
            }

            // set null package id for any 'package-less' deployments
            foreach (var package in packagesToDeploy.Where(package => string.IsNullOrEmpty(package.Id)))
            {
                package.Id = PackageDescription.NullPackageId;
            }

            // get the NuGet package to push to instance AND crack open for Its.Config deployment file
            this.LogAnnouncement("Downloading packages that are to be deployed => IDs: " + string.Join(",", packagesToDeploy.Select(_ => _.Id)));

            // get deployment details from Its.Config in the package
            var deploymentFileSearchPattern = string.Format(".config/{0}/DeploymentConfigurationWithStrategies.json", environment);

            this.LogAnnouncement("Extracting deployment configuration(s) for specified environment from packages (if present).");
            var packagedDeploymentConfigs = this.GetPackagedDeploymentConfigurations(packagesToDeploy, deploymentFileSearchPattern);
            foreach (var config in packagedDeploymentConfigs)
            {
                if (config.DeploymentConfiguration == null)
                {
                    this.LogAnnouncement("   - Did NOT find config in package for: " + config.Package.PackageDescription.GetIdDotVersionString());
                }
                else
                {
                    this.LogAnnouncement("   - Found config in package for: " + config.Package.PackageDescription.GetIdDotVersionString());
                }
            }

            // apply default values to any nulls
            this.LogAnnouncement("Applying default deployment configuration options.");
            var packagedConfigsWithDefaults = packagedDeploymentConfigs.ApplyDefaults(this.defaultDeploymentConfiguration);

            // flatten configs into a single config to deploy onto an instance
            this.LogAnnouncement("Flattening multiple deployment configurations.");
            var flattenedConfig = packagedConfigsWithDefaults.Select(_ => _.DeploymentConfiguration).ToList().Flatten();

            // apply overrides
            this.LogAnnouncement("Applying applicable overrides to the flattened deployment configuration.");
            var overriddenConfig = flattenedConfig.ApplyOverrides(deploymentConfigOverride);

            // set config to use for creation
            var configToCreateWith = overriddenConfig;

            // apply newly constructed configs across all configs
            var packagedDeploymentConfigsWithDefaultsAndOverrides =
                packagedDeploymentConfigs.OverrideDeploymentConfig(configToCreateWith);

            // determine if a message bus harness package should be included
            var harnessPackagedConfig = this.GetMessageBusHarnessPackagedDeploymentConfigurationIfNecessary(
                environment,
                packagedDeploymentConfigsWithDefaultsAndOverrides,
                configToCreateWith);

            if (harnessPackagedConfig != null)
            {
                this.LogAnnouncement("Configured message bus harness config included in deployed package list");
                packagedDeploymentConfigsWithDefaultsAndOverrides.Add(harnessPackagedConfig);
            }

            // terminate existing instances...
            this.RunActionWithTelemetry("Terminating Instances", () => this.TerminateInstancesBeingReplaced(packagesToDeploy.WithoutStrategies(), environment));

            var instanceCount = configToCreateWith.InstanceCount;
            if (instanceCount == 0)
            {
                instanceCount = 1; // in case there isn't a config and an empty instance is being created...
            }

            var items = Enumerable.Range(0, instanceCount);
            var tasks =
                items.Select(
                    instanceNumber =>
                    Task.Run(
                        () =>
                        this.CreateNumberedInstanceAsync(
                            instanceNumber,
                            packagesToDeploy,
                            environment,
                            instanceName,
                            instanceCount,
                            configToCreateWith,
                            packagedDeploymentConfigsWithDefaultsAndOverrides))).ToArray();

            Task.WaitAll(tasks);

            this.LogAnnouncement("Finished deployment.");
        }

        private async Task CreateNumberedInstanceAsync(
            int instanceNumber,
            ICollection<PackageDescriptionWithOverrides> packagesToDeploy,
            string environment,
            string instanceName,
            int instanceCount,
            DeploymentConfiguration configToCreateWith,
            ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides)
        {
            // create new aws instance(s)
            var numberedInstanceName = instanceCount == 1 ? instanceName : instanceName + "-" + instanceNumber;
            var createAnnouncementAddIn = instanceCount > 1
                                              ? "(" + (instanceNumber + 1) + "/" + instanceCount + ")"
                                              : string.Empty;
            this.LogAnnouncement("Creating new instance " + createAnnouncementAddIn + " => MachineName: " + numberedInstanceName, instanceNumber);

            var createdInstanceDescription =
                await
                this.cloudManager.CreateNewInstanceAsync(
                    environment,
                    numberedInstanceName,
                    configToCreateWith,
                    packagesToDeploy.Select(_ => _ as PackageDescription).ToList(),
                    configToCreateWith.DeploymentStrategy.IncludeInstanceInitializationScript);

            var systemSpecificDetailsAsString = string.Join(
                ",",
                createdInstanceDescription.SystemSpecificDetails.Select(_ => _.Key + "=" + _.Value).ToArray());
            var createdInstanceMessage =
                string.Format(
                    "Instance " + instanceNumber
                    + " - Created new instance => CloudName: {0}, ID: {1}, Private IP: {2}, System Specific Details: {3}",
                    createdInstanceDescription.Name,
                    createdInstanceDescription.Id,
                    createdInstanceDescription.PrivateIpAddress,
                    systemSpecificDetailsAsString);
            this.LogAnnouncement(createdInstanceMessage, instanceNumber);

            this.LogAnnouncement("Waiting for status checks to pass.", instanceNumber);
            await this.WaitUntilStatusChecksSucceedAsync(instanceNumber, createdInstanceDescription);

            if (configToCreateWith.DeploymentStrategy.RunSetupSteps)
            {
                this.LogAnnouncement("Waiting for Administrator password to be available (takes a few minutes for this).", instanceNumber);
                var machineManager =
                    await
                    this.RunFuncWithTelemetry(
                        "Wait for admin password",
                        async () => await this.GetMachineManagerForInstanceAsync(createdInstanceDescription),
                        instanceNumber);

                this.LogAnnouncement("Waiting for machine to be accessible via WinRM (requires connectivity - make sure VPN is up if applicable).", instanceNumber);
                this.RunActionWithTelemetry("Wait for WinRM access", () => this.WaitUntilMachineIsAccessible(machineManager), instanceNumber);

                var instanceLevelSetupSteps = this.setupStepFactory.GetInstanceLevelSetupSteps(createdInstanceDescription.ComputerName);
                this.LogAnnouncement("Running setup actions that finalize the instance creation.", instanceNumber);
                this.RunActionWithTelemetry("Run instance level setup steps", () => this.RunSetupSteps(machineManager, instanceLevelSetupSteps, instanceNumber), instanceNumber);

                // this is necessary for finishing start up items, might have to try a few times until WinRM is available...
                this.LogAnnouncement("Rebooting new instance to finalize any items from instance setup.", instanceNumber);
                this.RebootInstance(instanceNumber, machineManager);

                foreach (var packagedConfig in packagedDeploymentConfigsWithDefaultsAndOverrides)
                {
                    var setupSteps = this.setupStepFactory.GetSetupSteps(packagedConfig, environment);
                    this.LogAnnouncement("Running setup actions for package: " + packagedConfig.Package.PackageDescription.GetIdDotVersionString(), instanceNumber);

                    this.RunSetupSteps(machineManager, setupSteps, instanceNumber);

                    // Mark the instance as having the successfully deployed packages
                    this.tracker.ProcessDeployedPackage(
                        environment,
                        createdInstanceDescription.Id,
                        packagedConfig.Package.PackageDescription);
                }
            }

            // get all web initializations to update any DNS entries on the public IP address.
            var webInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .GetInitializationStrategiesOf<InitializationStrategyIis>();

            this.LogAnnouncement("Updating DNS for web initializations (if applicable)", instanceNumber);
            foreach (var webInitialization in webInitializations)
            {
                var ipAddress = createdInstanceDescription.PublicIpAddress
                                ?? createdInstanceDescription.PrivateIpAddress;
                var dns = ApplyDnsTokenReplacements(
                        webInitialization.PrimaryDns,
                        numberedInstanceName,
                        environment,
                        instanceNumber);

                this.LogAnnouncement(string.Format(" - Pointing {0} at {1}.", dns, ipAddress), instanceNumber);

                lock (this.syncDnsManager)
                {
                    this.cloudManager.UpsertDnsEntryAsync(
                        environment,
                        createdInstanceDescription.Location,
                        dns,
                        new[] { ipAddress }).Wait();
                }
            }

            // get all DNS initializations to update any private DNS entries on the private IP address.
            this.LogAnnouncement("Updating DNS for all DNS initializations (if applicable)", instanceNumber);
            var dnsInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .GetInitializationStrategiesOf<InitializationStrategyDnsEntry>();
            foreach (var initialization in dnsInitializations)
            {
                if (string.IsNullOrEmpty(initialization.PublicDnsEntry)
                    && string.IsNullOrEmpty(initialization.PrivateDnsEntry))
                {
                    throw new ArgumentException(
                        "Instance " + instanceNumber
                        + " - Cannot create DNS entry of empty or null string, please specify either a public or private dns entry.");
                }

                if (!string.IsNullOrEmpty(initialization.PrivateDnsEntry))
                {
                    var privateIpAddress = createdInstanceDescription.PrivateIpAddress;
                    var privateDnsEntry = ApplyDnsTokenReplacements(
                        initialization.PrivateDnsEntry,
                        numberedInstanceName,
                        environment,
                        instanceNumber);

                    var pointedPrivateDnsMessage =
                        string.Format(
                            " - Pointing {0} at {1}.",
                            privateDnsEntry,
                            privateIpAddress);
                    this.LogAnnouncement(pointedPrivateDnsMessage, instanceNumber);

                    lock (this.syncDnsManager)
                    {
                        this.cloudManager.UpsertDnsEntryAsync(
                            environment,
                            createdInstanceDescription.Location,
                            privateDnsEntry,
                            new[] { privateIpAddress }).Wait();
                    }
                }

                if (!string.IsNullOrEmpty(initialization.PublicDnsEntry))
                {
                    var publicIpAddress = createdInstanceDescription.PublicIpAddress;
                    if (string.IsNullOrEmpty(publicIpAddress))
                    {
                        throw new ArgumentException(
                            "Instance " + instanceNumber
                            + " - Cannot assign a public DNS because there isn't a public IP address on the instance.");
                    }

                    var publicDnsEntry = ApplyDnsTokenReplacements(
                        initialization.PublicDnsEntry,
                        numberedInstanceName,
                        environment,
                        instanceNumber);

                    var pointedPublicDnsMessage =
                        string.Format(
                            " - Pointing {0} at {1}.",
                            publicDnsEntry,
                            publicIpAddress);
                    this.LogAnnouncement(pointedPublicDnsMessage, instanceNumber);

                    lock (this.syncDnsManager)
                    {
                        this.cloudManager.UpsertDnsEntryAsync(
                            environment,
                            createdInstanceDescription.Location,
                            publicDnsEntry,
                            new[] { publicIpAddress }).Wait();
                    }
                }
            }

            if ((configToCreateWith.PostDeploymentStrategy ?? new PostDeploymentStrategy()).TurnOffInstance)
            {
                const bool WaitUntilOff = true;
                this.LogAnnouncement("Post deployment strategy: TurnOffInstance is true - shutting down instance.", instanceNumber);
                await this.cloudManager.TurnOffInstanceAsync(
                    createdInstanceDescription.Id,
                    createdInstanceDescription.Location,
                    // ReSharper disable once RedundantArgumentDefaultValue - keeping for clarity of what's happening...
                    WaitUntilOff);
            }
        }

        private PackagedDeploymentConfiguration GetMessageBusHarnessPackagedDeploymentConfigurationIfNecessary(
            string environment,
            ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides,
            DeploymentConfiguration configToCreateWith)
        {
            PackagedDeploymentConfiguration ret = null;

            // get all message bus handler initializations to know if we need a handler.
            var packagesWithMessageBusInitializations =
                packagedDeploymentConfigsWithDefaultsAndOverrides
                    .WhereContainsInitializationStrategyOf<InitializationStrategyMessageBusHandler>();

            var messageBusInitializations =
                packagesWithMessageBusInitializations.GetInitializationStrategiesOf<InitializationStrategyMessageBusHandler>();

            // make sure we're not already deploying the package ('server/host/schedule manager' is only scenario of this right now...)
            var alreadyDeployingTheSamePackageAsHandlersUse =
                packagedDeploymentConfigsWithDefaultsAndOverrides.Any(
                    _ => _.Package.PackageDescription.Id == this.messageBusHandlerHarnessConfiguration.Package.Id);

            var hasMessageBusInitializations = messageBusInitializations.Any();
            if (hasMessageBusInitializations && !alreadyDeployingTheSamePackageAsHandlersUse)
            {
                this.LogAnnouncement("Including MessageBusHandlerHarness in deployment since MessageBusHandlers are being deployed.");

                var itsConfigOverridesForHandlers = new List<ItsConfigOverride>();

                this.LogAnnouncement("Adding any Its.Config overrides AND/OR embedded Its.Config files from Message Handler package into Its.Config overrides of the Harness.");
                foreach (var packageWithMessageBusInitializations in packagesWithMessageBusInitializations)
                {
                    itsConfigOverridesForHandlers.AddRange(
                        packageWithMessageBusInitializations.ItsConfigOverrides ?? new List<ItsConfigOverride>());

                    var packageFolderName =
                        packageWithMessageBusInitializations.Package.PackageDescription.GetIdDotVersionString();

                    // extract appropriate files from 
                    var itsConfigFilesFromPackage = new Dictionary<string, string>();
                    var precedenceChain = new[] { environment }.ToList();
                    precedenceChain.AddRange(this.itsConfigPrecedenceAfterEnvironment);
                    foreach (var precedenceElement in precedenceChain)
                    {
                        var itsConfigFolderPattern = packageWithMessageBusInitializations.Package.AreDependenciesBundled
                                                         ? string.Format(
                                                             "{1}/.config/{0}/",
                                                             precedenceElement,
                                                             packageFolderName)
                                                         : string.Format(".config/{0}/", precedenceElement);

                        var itsConfigFilesFromPackageForPrecedenceElement =
                            this.packageManager.GetMultipleFileContentsFromPackageAsStrings(
                                packageWithMessageBusInitializations.Package,
                                itsConfigFolderPattern);

                        foreach (var item in itsConfigFilesFromPackageForPrecedenceElement)
                        {
                            itsConfigFilesFromPackage.Add(item.Key, item.Value);
                        }
                    }

                    itsConfigOverridesForHandlers.AddRange(
                        itsConfigFilesFromPackage.Select(
                            _ =>
                            new ItsConfigOverride
                            {
                                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(_.Key),
                                FileContentsJson = _.Value
                            }));
                }

                ret = this.BuildMessageBusHarnessPackagedConfig(
                    messageBusInitializations,
                    itsConfigOverridesForHandlers,
                    configToCreateWith);
            }
            else
            {
                var message = "No need for MessageBusHandlerHarness in deployment - HasMessageBusInitializations: "
                              + hasMessageBusInitializations + " AlreadyDeployingTheSamePackageAsHandlersUse: "
                              + alreadyDeployingTheSamePackageAsHandlersUse;
                this.LogAnnouncement(message);
            }

            return ret;
        }

        private async Task WaitUntilStatusChecksSucceedAsync(int instanceNumber, InstanceDescription createdInstanceDescription)
        {
            var sleepTimeInSeconds = 10d;
            var allChecksPassed = false;
            while (!allChecksPassed)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                var status = await this.cloudManager.GetInstanceStatusAsync(
                    createdInstanceDescription.Id,
                    createdInstanceDescription.Location);

                if (status.InstanceChecks.Any(_ => _.Value == CheckState.Failed))
                {
                    foreach (var instanceCheck in status.InstanceChecks)
                    {
                        this.LogAnnouncement("Instance Check; " + instanceCheck.Key + " = " + instanceCheck.Value, instanceNumber);
                    }

                    throw new DeploymentException("Failure in an instance check.");
                }

                if (status.SystemChecks.Any(_ => _.Value == CheckState.Failed))
                {
                    foreach (var systemCheck in status.SystemChecks)
                    {
                        this.LogAnnouncement("System Check; " + systemCheck.Key + " = " + systemCheck.Value, instanceNumber);
                    }

                    throw new DeploymentException("Failure in a system check.");
                }

                allChecksPassed = status.InstanceChecks.All(_ => _.Value == CheckState.Passed)
                                  && status.SystemChecks.All(_ => _.Value == CheckState.Passed);
            }
        }

        private void RunSetupSteps(MachineManager machineManager, ICollection<SetupStep> setupSteps, int instanceNumber)
        {
            var maxTries = this.setupStepFactory.MaxSetupStepAttempts;
            var throwOnFailedSetupStep = this.setupStepFactory.ThrowOnFailedSetupStep;

            foreach (var setupStep in setupSteps)
            {
                Action retryingSetupAction = () =>
                    {
                        this.RunSetupStepWithRetry(
                            machineManager,
                            instanceNumber,
                            setupStep,
                            maxTries,
                            throwOnFailedSetupStep);
                    };

                this.RunActionWithTelemetry("Setup step - " + setupStep.Description, retryingSetupAction, instanceNumber);
            }
        }

        private ICollection<PackagedDeploymentConfiguration> GetPackagedDeploymentConfigurations(
            ICollection<PackageDescriptionWithOverrides> packagesToDeploy,
            string deploymentFileSearchPattern)
        {
            Func<IHaveInitializationStrategies, bool> figureOutIfNeedToBundleDependencies =
                hasStrategies =>
                hasStrategies != null
                && (hasStrategies.GetInitializationStrategiesOf<InitializationStrategyMessageBusHandler>().Any()
                    || hasStrategies.GetInitializationStrategiesOf<InitializationStrategySqlServer>().Any());

            var packagedDeploymentConfigs = packagesToDeploy.Select(
                packageDescriptionWithOverrides =>
                    {
                        // decide whether we need to get all of the dependencies or just the normal package
                        // currently this is for message bus handlers since web services already include all assemblies...
                        var bundleAllDependencies = figureOutIfNeedToBundleDependencies(packageDescriptionWithOverrides);
                        var package = this.packageManager.GetPackage(packageDescriptionWithOverrides, bundleAllDependencies);
                        var actualVersion = this.GetActualVersionFromPackage(package);

                        var deploymentConfigJson =
                            this.packageManager.GetMultipleFileContentsFromPackageAsStrings(
                                package,
                                deploymentFileSearchPattern).Select(_ => _.Value).SingleOrDefault();
                        var deploymentConfig =
                            Serializer.Deserialize<DeploymentConfigurationWithStrategies>(
                                (deploymentConfigJson ?? string.Empty).Replace("\ufeff", string.Empty)); // strip the BOM as it makes Newtonsoft bomb...

                        // re-check the extracted config to make sure it doesn't change the decision about bundling...
                        var bundleAllDependenciesReCheck = figureOutIfNeedToBundleDependencies(deploymentConfig);

                        if (!bundleAllDependencies && bundleAllDependenciesReCheck)
                        {
                            // since we previously didn't bundle the packages dependencies 
                            //        AND found in the extraced config that we need to 
                            //       THEN we need to re-download with dependencies bundled...
                            package = this.packageManager.GetPackage(
                                packageDescriptionWithOverrides,
                                true);
                        }

                        // take overrides if present, otherwise take existing, otherwise take empty
                        var initializationStrategies = packageDescriptionWithOverrides.InitializationStrategies != null
                                                       && packageDescriptionWithOverrides.InitializationStrategies.Count
                                                       > 0
                                                           ? packageDescriptionWithOverrides.InitializationStrategies
                                                           : (deploymentConfig
                                                              ?? new DeploymentConfigurationWithStrategies())
                                                                 .InitializationStrategies
                                                             ?? new List<InitializationStrategyBase>();

                        // Overwrite w/ specific version if able to find...
                        package.PackageDescription.Version = actualVersion;

                        var newItem = new PackagedDeploymentConfiguration
                                          {
                                              Package = package,
                                              DeploymentConfiguration = deploymentConfig,
                                              InitializationStrategies =
                                                  initializationStrategies,
                                              ItsConfigOverrides =
                                                  packageDescriptionWithOverrides
                                                  .ItsConfigOverrides,
                                          };
                        return newItem;
                    }).ToList();

            return packagedDeploymentConfigs;
        }

        private void RunSetupStepWithRetry(
            MachineManager machineManager,
            int instanceNumber,
            SetupStep setupStep,
            int maxTries,
            bool throwOnFailedSetupStep)
        {
            this.LogAnnouncement("  - " + setupStep.Description, instanceNumber);
            var tries = 0;
            while (tries < maxTries)
            {
                try
                {
                    tries = tries + 1;
                    lock (this.syncMachineManager)
                    {
                        setupStep.SetupAction(machineManager);
                    }

                    break;
                }
                catch (Exception ex)
                {
                    if (tries >= maxTries)
                    {
                        if (throwOnFailedSetupStep)
                        {
                            throw new DeploymentException(
                                "Instance " + instanceNumber + " - Failed to run setup step " + setupStep.Description
                                + " after " + maxTries + " attempts",
                                ex);
                        }
                        else
                        {
                            var exceededMaxTriesMessage =
                                string.Format(
                                    "Step {3} - Exception on try {0}/{1} - {2}",
                                    tries,
                                    maxTries,
                                    ex,
                                    setupStep.Description);
                            this.LogAnnouncement(exceededMaxTriesMessage, instanceNumber);
                        }
                    }
                    else
                    {
                        var failedRetryingMessage =
                            string.Format(
                                "Step {2} - Exception on try {0}/{1} - retrying",
                                tries,
                                maxTries,
                                setupStep.Description);
                        this.LogAnnouncement(failedRetryingMessage, instanceNumber);
                        Thread.Sleep(TimeSpan.FromSeconds(tries * 10));
                    }
                }
            }
        }

        private string GetActualVersionFromPackage(Package package)
        {
            if (string.Equals(
                package.PackageDescription.Id,
                PackageDescription.NullPackageId,
                StringComparison.CurrentCultureIgnoreCase))
            {
                return "[DOES NOT HAVE A VERSION]";
            }

            var nuSpecSearchPattern = package.PackageDescription.Id + ".nuspec";
            var nuSpecFileContents =
                this.packageManager.GetMultipleFileContentsFromPackageAsStrings(package, nuSpecSearchPattern)
                    .Select(_ => _.Value)
                    .SingleOrDefault();
            var actualVersion = nuSpecFileContents == null
                                    ? "[FAILED TO EXTRACT FROM PACKAGE]"
                                    : this.packageManager.GetVersionFromNuSpecFile(nuSpecFileContents);
            return actualVersion;
        }

        private PackagedDeploymentConfiguration BuildMessageBusHarnessPackagedConfig(ICollection<InitializationStrategyMessageBusHandler> messageBusInitializations, ICollection<ItsConfigOverride> itsConfigOverrides, DeploymentConfiguration configToCreateWith)
        {
            // TODO:    Maybe this should be exclusively done with that provided package and 
            // TODO:        only update the private channel to monitor and directory of packages...

            // Create a new list to use for the overrides of the handler harness deployment
            var itsConfigOverridesToUse = new List<ItsConfigOverride>();
            if (itsConfigOverrides != null)
            {
                // merge in any ItsConfig overrides supplied with handler packages
                itsConfigOverridesToUse.AddRange(itsConfigOverrides);
            }

            if (this.messageBusHandlerHarnessConfiguration.Package.ItsConfigOverrides != null)
            {
                // merge in any overrides specified with the handler package
                itsConfigOverridesToUse.AddRange(this.messageBusHandlerHarnessConfiguration.Package.ItsConfigOverrides);
            }

            var messageBusHandlerPackage =
                this.packageManager.GetPackage(this.messageBusHandlerHarnessConfiguration.Package);

            var actualVersion = this.GetActualVersionFromPackage(messageBusHandlerPackage);
            messageBusHandlerPackage.PackageDescription.Version = actualVersion;

            var channelsToMonitor = messageBusInitializations.SelectMany(_ => _.ChannelsToMonitor).Distinct().ToList();

            var workerCount = messageBusInitializations.Min(_ => _.WorkerCount);
            workerCount = workerCount == 0 ? 1 : workerCount;

            var executorRoleSettings = new[]
                                           {
                                               new MessageBusHarnessRoleSettingsExecutor
                                                   {
                                                       ChannelsToMonitor =
                                                           channelsToMonitor,
                                                       HandlerAssemblyPath =
                                                           this.setupStepFactory
                                                           .RootDeploymentPath,
                                                       WorkerCount = workerCount,
                                                       PollingTimeSpan =
                                                           TimeSpan.FromMinutes(1),
                                                       TypeMatchStrategy = TypeMatchStrategy.NamespaceAndName,
                                                       MessageDispatcherWaitThreadSleepTime = TimeSpan.FromSeconds(.5),
                                                       RetryCount = 0,
                                                       HarnessProcessTimeToLive = this.messageBusHandlerHarnessConfiguration.HandlerHarnessProcessTimeToLive,
                                                       InvisibilityTimeout = this.messageBusHandlerHarnessConfiguration.InvisibilityTimeout
                                                   }
                                           };

            var messageBusHandlerSettings = new MessageBusHarnessSettings
                                                {
                                                    PersistenceConnectionString = this.messageBusPersistenceConnectionString,
                                                    RoleSettings = executorRoleSettings,
                                                    LogProcessorSettings = this.messageBusHandlerHarnessConfiguration.LogProcessorSettings
                                                };

            // add the override that will activate the harness in executor mode.
            var messageBusHandlerSettingsJson = Serializer.Serialize(messageBusHandlerSettings);
            itsConfigOverridesToUse.Add(
                new ItsConfigOverride
                    {
                        FileNameWithoutExtension = "MessageBusHarnessSettings",
                        FileContentsJson = messageBusHandlerSettingsJson
                    });

            var messageBusHandlerHarnessInitializationStrategies =
                this.messageBusHandlerHarnessConfiguration.Package.InitializationStrategies.Select(
                    _ => (InitializationStrategyBase)_.Clone()).ToList();
            var harnessPackagedConfig = new PackagedDeploymentConfiguration
                                            {
                                                DeploymentConfiguration =
                                                    configToCreateWith,
                                                Package = messageBusHandlerPackage,
                                                ItsConfigOverrides =
                                                    itsConfigOverridesToUse,
                                                InitializationStrategies =
                                                    messageBusHandlerHarnessInitializationStrategies,
                                            };

            return harnessPackagedConfig;
        }

        private static string ApplyDnsTokenReplacements(string potentiallyTokenizedDns, string instanceName, string environment, int instanceNumber)
        {
            var ret = TokenSubstitutions.GetSubstitutedStringForDns(
                potentiallyTokenizedDns,
                environment,
                instanceName,
                instanceNumber);
            return ret;
        }

        private void RebootInstance(int instanceNumber, MachineManager machineManager)
        {
            var sleepTimeInSeconds = 1d;
            var rebootCallSucceeded = false;
            var tries = 0;
            while (!rebootCallSucceeded)
            {
                tries = tries + 1;
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                try
                {
                    lock (this.syncMachineManager)
                    {
                        machineManager.Reboot();
                    }

                    rebootCallSucceeded = true;
                }
                catch (Exception ex)
                {
                    if (tries > 100)
                    {
                        this.LogAnnouncement(ex.ToString(), instanceNumber);
                    }
                }
            }

            this.LogAnnouncement("Waiting for machine to come back up from reboot.", instanceNumber);
            this.WaitUntilMachineIsAccessible(machineManager);
        }

        private void WaitUntilMachineIsAccessible(MachineManager machineManager)
        {
            // TODO: move to machineManager.BlockUntilAvailable(TimeSpan.Zero);
            var sleepTimeInSeconds = 10d;
            var reachable = false;
            while (!reachable)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                try
                {
                    lock (this.syncMachineManager)
                    {
                        // ReSharper disable once UnusedVariable - keeping to show that there is a return and intentionally ignoring...
                        var notNeededResults = machineManager.RunScript(@"{ ls C:\Windows }");
                    }

                    reachable = true;
                }
                catch (Exception)
                {
                    /* no-op */
                }
            }
        }

        private async Task<MachineManager> GetMachineManagerForInstanceAsync(InstanceDescription createdInstanceDescription)
        {
            string adminPassword = null;
            var sleepTimeInSeconds = 30d;
            var privateKey = this.tracker.GetPrivateKeyOfInstanceById(createdInstanceDescription.Environment, createdInstanceDescription.Id);
            while (adminPassword == null)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                try
                {
                    adminPassword = await this.cloudManager.GetAdministratorPasswordForInstanceAsync(
                        createdInstanceDescription,
                        privateKey);
                }
                catch (NullPasswordDataException)
                {
                    // No-op
                }
            }

            // finalize instance creation (WinRM reboot, etc.)
            var machineManager = new MachineManager(
                createdInstanceDescription.PrivateIpAddress,
                "Administrator",
                MachineManager.ConvertStringToSecureString(adminPassword),
                true);
            return machineManager;
        }

        private void TerminateInstancesBeingReplaced(ICollection<PackageDescription> packagesToDeploy, string environment)
        {
            var packagesToIgnore = this.packageIdsToIgnoreDuringTerminationSearch.Select(_ => new PackageDescription { Id = _ }).ToList();

            // get aws instance object by name (from the AWS object tracking storage)
            var packagesToCheckFor = packagesToDeploy.Except(packagesToIgnore, new PackageDescriptionIdOnlyEqualityComparer()).ToList();
            var instancesMatchingPackagesAllEnvironments =
                this.tracker.GetInstancesByDeployedPackages(environment, packagesToCheckFor).ToList();
            var instancesWithMatchingEnvironmentAndPackages =
                instancesMatchingPackagesAllEnvironments.Where(
                    _ => string.Equals(_.Environment, environment, StringComparison.CurrentCultureIgnoreCase)).ToList();

            // confirm that terminating the instances will not take down any packages that aren't getting re-deployed...
            var deployedPackagesToCheck =
                instancesWithMatchingEnvironmentAndPackages.SelectMany(_ => _.DeployedPackages.Values)
                    .Except(packagesToIgnore, new PackageDescriptionIdOnlyEqualityComparer())
                    .ToList();
            if (deployedPackagesToCheck.Except(packagesToDeploy, new PackageDescriptionIdOnlyEqualityComparer()).Any())
            {
                var deployedIdList = string.Join(",", deployedPackagesToCheck.Select(_ => _.Id));
                var deployingIdList = string.Join(",", packagesToDeploy.Select(_ => _.Id));
                throw new DeploymentException(
                    "Cannot proceed because taking down the instances of requested packages will take down packages not getting redeployed => Running: "
                    + deployedIdList + " Deploying: " + deployingIdList);
            }

            // terminate instance(s) if necessary (if it exists)
            if (instancesWithMatchingEnvironmentAndPackages.Any())
            {
                var tasks =
                    instancesWithMatchingEnvironmentAndPackages.Select(
                        instanceDescription => Task.Run(() => this.TerminateInstance(environment, instanceDescription)))
                        .ToArray();

                Task.WaitAll(tasks);
            }
            else
            {
                this.LogAnnouncement("Did not find any existing instances for the specified package list and environment.");
            }
        }

        private async Task TerminateInstance(string environment, InstanceDescription instanceDescription)
        {
            this.LogAnnouncement("Terminating instance => ID: " + instanceDescription.Id + ", CloudName: " + instanceDescription.Name);
            await
                this.cloudManager.TerminateInstanceAsync(
                    environment,
                    instanceDescription.Id,
                    instanceDescription.Location,
                    true);
        }

        /// <summary>
        /// Gets the password for the instance.
        /// </summary>
        /// <param name="instanceToSearchFor">Instance to find the password for.</param>
        /// <returns>Password for instance.</returns>
        public async Task<string> GetPasswordAsync(InstanceDescription instanceToSearchFor)
        {
            string adminPassword = null;
            var sleepTimeInSeconds = .25;
            var privateKey = this.tracker.GetPrivateKeyOfInstanceById(instanceToSearchFor.Environment, instanceToSearchFor.Id);
            while (adminPassword == null)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 2;
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));
                adminPassword = await this.cloudManager.GetAdministratorPasswordForInstanceAsync(instanceToSearchFor, privateKey);
            }

            return adminPassword;
        }

        private void RunActionWithTelemetry(string step, Action code, int? instanceNumber = null)
        {
            this.StartTelemetry(step, instanceNumber);
            code();
            this.StopTelemetry(step, instanceNumber);
        }

        private T RunFuncWithTelemetry<T>(string step, Func<T> code, int? instanceNumber = null)
        {
            this.StartTelemetry(step, instanceNumber);
            T ret = code();
            this.StopTelemetry(step, instanceNumber);
            return ret;
        }

        private void StartTelemetry(string step, int? instanceNumber = null)
        {
            if (this.telemetryFile == null)
            {
                return;
            }

            var entry = new TelemetryEntry { InstanceNumber = instanceNumber, Step = step, Start = DateTime.UtcNow, };
            this.telemetry.Add(entry);
            this.FlushTelemetry();
        }

        private void StopTelemetry(string step, int? instanceNumber = null)
        {
            if (this.telemetryFile == null)
            {
                return;
            }

            var stopTime = DateTime.UtcNow;
            var currentEntry = this.telemetry.Single(_ => _.InstanceNumber == instanceNumber && _.Step == step && _.Stop == null);
            currentEntry.Stop = stopTime;
            this.FlushTelemetry();
        }

        private void FlushTelemetry()
        {
            if (this.telemetryFile == null)
            {
                return;
            }

            var objectToFlush =
                this.telemetry.GroupBy(_ => _.InstanceNumber)
                    .OrderBy(_ => _.Key)
                    .Select(instanceGroup => instanceGroup.OrderBy(item => item.Start))
                    .SelectMany(_ => _)
                    .ToList();

            var json = JsonConvert.SerializeObject(objectToFlush);

            lock (this.telemetryFile)
            {
                File.WriteAllText(this.telemetryFile, json);
            }
        }

        private void LogAnnouncement(string step, int? instanceNumber = null)
        {
            if (this.announcementFile == null)
            {
                return;
            }

            var time = DateTime.UtcNow;

            var instanceNumberAdjustedStep = instanceNumber != null ? "Instance " + instanceNumber + " - " + step : step;
            this.announce(instanceNumberAdjustedStep);
            var entry = new AnnouncementEntry { DateTime = time, InstanceNumber = instanceNumber, Step = step };
            this.announcements.Add(entry);
            this.FlushAnnouncements();
        }

        private void FlushAnnouncements()
        {
            if (this.announcementFile == null)
            {
                return;
            }

            var objectToFlush =
                this.announcements.GroupBy(_ => _.InstanceNumber)
                    .OrderBy(_ => _.Key)
                    .Select(instanceGroup => instanceGroup.OrderBy(item => item.DateTime))
                    .SelectMany(_ => _)
                    .ToList();

            var json = JsonConvert.SerializeObject(objectToFlush);

            lock (this.announcementFile)
            {
                File.WriteAllText(this.announcementFile, json);
            }
        }

        private class TelemetryEntry 
        {
            public int? InstanceNumber { get; set; }

            public string Step { get; set; }

            public DateTime? Start { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local - want this for serialization...
            public DateTime? Stop { get; set; }
        }

        private class AnnouncementEntry 
        {
            public int? InstanceNumber { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local - want this for serialization...
            public string Step { get; set; }

            public DateTime? DateTime { get; set; }
        }
    }
}
