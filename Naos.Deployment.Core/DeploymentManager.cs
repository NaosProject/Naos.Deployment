// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentManager.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Naos.Deployment.Domain;
    using Naos.MachineManagement.Domain;
    using Naos.Packaging.Domain;
    using Naos.Recipes.RunWithRetry;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Serialization;
    using OBeautifulCode.Serialization.Json;
    using static System.FormattableString;

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Not refactoring right now.")]
    public class DeploymentManager : IManageDeployments
    {
        private static readonly IStringSerialize AnnouncementSerializer = new ObcJsonSerializer(typeof(NaosDeploymentCoreJsonSerializationConfiguration).ToJsonSerializationConfigurationType());

        /// <summary>
        /// Lock object to only allow one DNS update at a time because AWSSDK does not seem to support this otherwise.
        /// </summary>
        private readonly object syncDnsManager = new object();

        /// <summary>
        /// Lock object to only allow one WinRM call to happen at a time because System.Management.Automation seems to *sometimes* not support this.
        /// </summary>
        private readonly object syncMachineManager = new object();

        private readonly object syncAnnounmcment = new object();
        private readonly object syncDebugAnnounmcment = new object();
        private readonly object syncTelemetry = new object();

        private readonly ITrackComputingInfrastructure tracker;

        private readonly IManageComputingInfrastructure computingManager;

        private readonly IGetPackages packageManager;

        private readonly DeploymentConfiguration defaultDeploymentConfiguration;

        private readonly Action<string> announce;

        private readonly Action<string> debugAnnouncer;

        private readonly SetupStepFactory setupStepFactory;

        private readonly DeploymentAdjustmentStrategiesApplicator deploymentAdjustmentStrategiesApplicator;

        private readonly IReadOnlyCollection<string> packageIdsToIgnoreDuringTerminationSearch;

        private readonly ConcurrentBag<TelemetryEntry> telemetry = new ConcurrentBag<TelemetryEntry>();

        private readonly string telemetryFile;

        private readonly ConcurrentBag<AnnouncementEntry> announcements = new ConcurrentBag<AnnouncementEntry>();

        private readonly string announcementFile;

        private readonly ConcurrentBag<DebugAnnouncementEntry> debugAnnouncements = new ConcurrentBag<DebugAnnouncementEntry>();

        private readonly string debugAnnouncementFile;

        private readonly PackageHelper packageHelper;

        private readonly IManageConfigFiles configFileManager;

        private readonly ICreateMachineManagers machineManagerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentManager"/> class.
        /// </summary>
        /// <param name="tracker">Tracker of computing infrastructure.</param>
        /// <param name="computingManager">Manager of the computing infrastructure (wraps custom computing interactions).</param>
        /// <param name="packageManager">Proxy to retrieve packages.</param>
        /// <param name="certificateRetriever">Manager of certificates (get passwords and file bytes by name).</param>
        /// <param name="defaultDeploymentConfiguration">Default deployment configuration to substitute the values for any nulls.</param>
        /// <param name="setupStepFactorySettings">Settings for the setup step factory.</param>
        /// <param name="deploymentAdjustmentStrategiesApplicator">Connection string to the message bus harness.</param>
        /// <param name="packageIdsToIgnoreDuringTerminationSearch">List of package IDs to exclude during replacement search.</param>
        /// <param name="announcer">Callback to get status messages through process.</param>
        /// <param name="debugAnnouncer">Callback to get more detailed information through process.</param>
        /// <param name="workingDirectory">Directory to perform temp disk operations.</param>
        /// <param name="configFileManager">Config file manager necessary to be able to provide Its.Configuration overrides correctly.</param>
        /// <param name="machineManagerFactory">Machine manager factory.</param>
        /// <param name="environmentCertificateName">Optional name of the environment certificate to be found in the CertificateManager provided.</param>
        /// <param name="announcementFile">Optional file path to record a JSON file of announcements.</param>
        /// <param name="debugAnnouncementFile">Optional file path to record a JSON file of debug announcements.</param>
        /// <param name="telemetryFile">Optional file path to record JSON file of certain task timings.</param>
        public DeploymentManager(
            ITrackComputingInfrastructure tracker,
            IManageComputingInfrastructure computingManager,
            IGetPackages packageManager,
            IGetCertificates certificateRetriever,
            DefaultDeploymentConfiguration defaultDeploymentConfiguration,
            SetupStepFactorySettings setupStepFactorySettings,
            DeploymentAdjustmentStrategiesApplicator deploymentAdjustmentStrategiesApplicator,
            IReadOnlyCollection<string> packageIdsToIgnoreDuringTerminationSearch,
            Action<string> announcer,
            Action<string> debugAnnouncer,
            string workingDirectory,
            IManageConfigFiles configFileManager,
            ICreateMachineManagers machineManagerFactory,
            string environmentCertificateName = null,
            string announcementFile = null,
            string debugAnnouncementFile = null,
            string telemetryFile = null)
        {
            new { configFileManager }.AsArg().Must().NotBeNull();
            new { machineManagerFactory }.AsArg().Must().NotBeNull();

            this.tracker = tracker;
            this.computingManager = computingManager;
            this.packageManager = packageManager;
            this.defaultDeploymentConfiguration = defaultDeploymentConfiguration;
            this.deploymentAdjustmentStrategiesApplicator = deploymentAdjustmentStrategiesApplicator;
            this.packageIdsToIgnoreDuringTerminationSearch = packageIdsToIgnoreDuringTerminationSearch;
            this.announce = announcer;
            this.debugAnnouncer = debugAnnouncer;
            this.telemetryFile = telemetryFile;
            this.announcementFile = announcementFile;
            this.debugAnnouncementFile = debugAnnouncementFile;
            this.configFileManager = configFileManager;
            this.machineManagerFactory = machineManagerFactory;

            this.setupStepFactory = new SetupStepFactory(
                setupStepFactorySettings,
                certificateRetriever,
                packageManager,
                this.configFileManager,
                environmentCertificateName,
                workingDirectory,
                this.debugAnnouncer);

            this.packageHelper = new PackageHelper(packageManager, this.setupStepFactory.RootPackageDirectoriesToPrune, workingDirectory);
        }

        /// <inheritdoc />
        public async Task DeployPackagesAsync(IReadOnlyCollection<PackageDescriptionWithOverrides> packagesToDeploy, string environment, string instanceName, ExistingDeploymentStrategy existingDeploymentStrategy = ExistingDeploymentStrategy.Replace, DeploymentConfiguration deploymentConfigOverride = null)
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
                instanceName = string.Join("---", packagesToDeploy.Select(_ => _.PackageDescription.Id.Replace(".", "-")).ToArray());
            }

            // set null package id for any 'package-less' deployments
            foreach (var package in packagesToDeploy.Where(package => string.IsNullOrEmpty(package.PackageDescription.Id)))
            {
                package.PackageDescription.Id = PackageDescription.NullPackageId;
            }

            // get the NuGet package to push to instance AND crack open for Its.Config deployment file
            this.LogAnnouncement("Downloading packages that are to be deployed => IDs: " + string.Join(",", packagesToDeploy.Select(_ => _.PackageDescription.Id)));

            this.LogAnnouncement("Extracting deployment configuration(s) for specified environment from packages (if present).");
            var packagedDeploymentConfigs = GetPackagedDeploymentConfigurations(
                environment,
                this.packageManager,
                this.packageHelper,
                this.configFileManager,
                packagesToDeploy);

            foreach (var config in packagedDeploymentConfigs)
            {
                if (config.DeploymentConfiguration == null)
                {
                    this.LogAnnouncement("   - Did NOT find config in package for: " + config.PackageWithBundleIdentifier.Package.PackageDescription.GetIdDotVersionString());
                }
                else
                {
                    this.LogAnnouncement("   - Found config in package for: " + config.PackageWithBundleIdentifier.Package.PackageDescription.GetIdDotVersionString());
                }
            }

            var configToCreateWith = PrepareAndConsolidateConfigs(this.defaultDeploymentConfiguration, deploymentConfigOverride, packagedDeploymentConfigs, input => this.LogAnnouncement(input));

            // apply newly constructed configs across all configs
            var packagedDeploymentConfigsWithDefaultsAndOverrides = packagedDeploymentConfigs.OverrideDeploymentConfig(configToCreateWith);

            var potentiallyAlteredInstanceName = await this.RunFuncWithTelemetryAsync(
                "Terminating Instances",
                () => this.ProcessExistingDeploymentStrategyAsync(
                    packagesToDeploy.WithoutStrategies(),
                    environment,
                    existingDeploymentStrategy,
                    instanceName));

            var instanceCount = configToCreateWith.InstanceCount;
            if (instanceCount == 0)
            {
                instanceCount = 1; // in case there isn't a config and an empty instance is being created...
            }

            var items = Enumerable.Range(0, instanceCount);
            var tasks =
                items.Select(
                    instanceNumber =>
                    this.CreateNumberedInstanceAsync(
                        existingDeploymentStrategy,
                        instanceNumber,
                        packagesToDeploy,
                        environment,
                        potentiallyAlteredInstanceName,
                        instanceCount,
                        configToCreateWith,
                        packagedDeploymentConfigsWithDefaultsAndOverrides)).ToArray();

            await Task.WhenAll(tasks);

            this.LogAnnouncement("Finished deployment.");
        }

        /// <summary>
        /// Prepare configurations then consolidate and finish any preparation.
        /// </summary>
        /// <param name="defaultDeploymentConfig">Default config.</param>
        /// <param name="overrideDeploymentConfig">Override config.</param>
        /// <param name="packagedDeploymentConfigs">Packages with deployment configuration extracted.</param>
        /// <param name="announcer">Optional announcer for logic applied.</param>
        /// <returns>Prepared and consolidated deployment configuration.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Configs", Justification = "Spelling/name is correct.")]
        public static DeploymentConfiguration PrepareAndConsolidateConfigs(
            DeploymentConfiguration defaultDeploymentConfig,
            DeploymentConfiguration overrideDeploymentConfig,
            IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigs,
            Action<string> announcer = null)
        {
            var localAnnouncer = announcer ?? (s => { /* no-op */ });

            // apply default values to any nulls
            localAnnouncer("Applying default deployment configuration options.");
            var packagedConfigsWithDefaults = packagedDeploymentConfigs.ApplyDefaults(defaultDeploymentConfig);

            // flatten configs into a single config to deploy onto an instance
            localAnnouncer("Flattening multiple deployment configurations.");
            var flattenedConfig = packagedConfigsWithDefaults.Select(_ => _.DeploymentConfiguration).ToList().Flatten();

            // apply overrides
            localAnnouncer("Applying applicable overrides to the flattened deployment configuration.");
            var overriddenConfig = flattenedConfig.ApplyOverrides(overrideDeploymentConfig);

            // set config to use for creation
            var configToCreateWith = overriddenConfig;
            return configToCreateWith;
        }

        private async Task CreateNumberedInstanceAsync(
            ExistingDeploymentStrategy existingDeploymentStrategy,
            int instanceNumber,
            IReadOnlyCollection<PackageDescriptionWithOverrides> packagesToDeploy,
            string environment,
            string instanceName,
            int instanceCount,
            DeploymentConfiguration configToCreateWith,
            IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides)
        {
            // create new aws instance(s)
            var numberedInstanceName = instanceCount == 1 ? instanceName : instanceName + "-" + instanceNumber;
            var createAnnouncementAddIn = instanceCount > 1
                                              ? "(" + (instanceNumber + 1) + "/" + instanceCount + ")"
                                              : string.Empty;
            this.LogAnnouncement("Creating new instance " + createAnnouncementAddIn + " => MachineName: " + numberedInstanceName, instanceNumber);

            var createdInstanceDescription =
                await
                this.computingManager.CreateNewInstanceAsync(
                    environment,
                    numberedInstanceName,
                    configToCreateWith,
                    packagesToDeploy,
                    configToCreateWith.DeploymentStrategy.IncludeInstanceInitializationScript);

            var systemSpecificDetailsAsString = string.Join(
                ",",
                (createdInstanceDescription.SystemSpecificDetails ?? new Dictionary<string, string>()).Select(_ => _.Key + "=" + _.Value).ToArray());
            var createdInstanceMessage =
                $"Instance {instanceNumber} - Created new instance => ComputingName: {createdInstanceDescription.Name}, ID: {createdInstanceDescription.Id}, Private IP: {createdInstanceDescription.PrivateIpAddress}, Public IP: {createdInstanceDescription.PublicIpAddress}, System Specific Details: {systemSpecificDetailsAsString}";
            this.LogAnnouncement(createdInstanceMessage, instanceNumber);

            this.LogAnnouncement("Waiting for status checks to pass.", instanceNumber);
            await this.WaitUntilStatusChecksSucceedAsync(instanceNumber, createdInstanceDescription);

            var deploymentDriveLetter = this.setupStepFactory.Settings.GetDeploymentDriveLetter(configToCreateWith.Volumes);
            var harnessAccount = this.setupStepFactory.Settings.HarnessSettings.HarnessAccount;
            var iisAccount = this.setupStepFactory.Settings.WebServerSettings.IisAccount;
            string FuncToReplaceSupportedTokens(string tokenizedString)
            {
                var workingString = tokenizedString;
                workingString = TokenSubstitutions.GetSubstitutedStringForAccounts(
                    workingString,
                    harnessAccount,
                    iisAccount);

                workingString = TokenSubstitutions.GetSubstitutedStringForChannelName(
                    workingString,
                    environment,
                    numberedInstanceName,
                    instanceNumber);

                workingString = TokenSubstitutions.GetSubstitutedStringForDns(
                    workingString,
                    environment,
                    numberedInstanceName,
                    instanceNumber);

                workingString = TokenSubstitutions.GetSubstitutedStringForPath(
                    workingString,
                    deploymentDriveLetter);

                return workingString;
            }

            // soft clone the list b/c we don't want to mess up the collection for other threads running different instance numbers
            var packagedDeploymentConfigsWithDefaultsAndOverridesAndHarness = packagedDeploymentConfigsWithDefaultsAndOverrides.Select(_ => _).ToList();
            if (configToCreateWith.DeploymentStrategy.RunSetupSteps)
            {
                this.LogAnnouncement("Waiting for Administrator password to be available (takes a few minutes for this).", instanceNumber);

                var adminPasswordClear =
                    await
                    this.RunFuncWithTelemetryAsync(
                        "Wait for admin password",
                        () => this.GetAdminPasswordForInstanceAsync(createdInstanceDescription),
                        instanceNumber);

                var machineManager = this.machineManagerFactory.CreateMachineManager(
                    configToCreateWith.InstanceType.OperatingSystem.MachineProtocol,
                    createdInstanceDescription.PrivateIpAddress,
                    this.setupStepFactory.AdministratorAccount,
                    adminPasswordClear);

                var additionalPackages = this.deploymentAdjustmentStrategiesApplicator.IdentifyAdditionalPackages(
                    environment,
                    instanceName,
                    instanceNumber,
                    this.configFileManager,
                    packagedDeploymentConfigsWithDefaultsAndOverrides,
                    configToCreateWith,
                    this.packageHelper,
                    this.setupStepFactory.Settings);

                if (additionalPackages.Any())
                {
                    additionalPackages.ToList().ForEach(
                        _ =>
                            {
                                this.LogAnnouncement("Identified Additional Package to add; " + _.Reason);
                                packagedDeploymentConfigsWithDefaultsAndOverridesAndHarness.Add(_.PackagedConfig);
                            });
                }

                var setupSteps = this.GetWaitUntilMachineIsReachableSteps(machineManager).ToList();

                var instanceLevelSetupSteps =
                    await
                    this.setupStepFactory.GetInstanceLevelSetupSteps(
                        createdInstanceDescription.ComputerName,
                        configToCreateWith.InstanceType.OperatingSystem,
                        environment,
                        packagedDeploymentConfigsWithDefaultsAndOverrides.SelectMany(_ => _.InitializationStrategies).ToList(),
                        this.setupStepFactory.Settings.BuildRootDeploymentPath(configToCreateWith.Volumes),
                        FuncToReplaceSupportedTokens);
                setupSteps.AddRange(instanceLevelSetupSteps);

                var chocoSetupSteps = this.setupStepFactory.GetChocolateySetupSteps(configToCreateWith.ChocolateyPackages);
                setupSteps.AddRange(chocoSetupSteps);

                var rebootSetupSteps = this.GetRebootSteps();
                setupSteps.AddRange(rebootSetupSteps);

                var blockUntilRebooted = this.GetBlockedUntilRebootedSteps();
                setupSteps.AddRange(blockUntilRebooted);

                foreach (var packagedConfig in packagedDeploymentConfigsWithDefaultsAndOverridesAndHarness)
                {
                    var packageDescription = packagedConfig.PackageWithBundleIdentifier.Package.PackageDescription;
                    var strategySetupSteps = await this.setupStepFactory.GetSetupStepsAsync(packagedConfig, environment, adminPasswordClear, FuncToReplaceSupportedTokens);
                    var updateArcologySetupSteps = this.GetSetupStepsToUpdateArcologyAfterPackageIsDeployed(
                        packageDescription,
                        createdInstanceDescription.Id,
                        environment);

                    var packageSetupSteps = new SetupStepBatch[0].Concat(strategySetupSteps).Concat(updateArcologySetupSteps).ToList();
                    setupSteps.AddRange(packageSetupSteps);
                }

                var updateDnsSteps = this.GetUpdateDnsSteps(
                    instanceNumber,
                    environment,
                    packagedDeploymentConfigsWithDefaultsAndOverridesAndHarness,
                    createdInstanceDescription,
                    FuncToReplaceSupportedTokens);
                if (updateDnsSteps.Any())
                {
                    // should ONLY update DNS when there is NO potential for a collision...
                    if (existingDeploymentStrategy == ExistingDeploymentStrategy.Replace)
                    {
                        setupSteps.AddRange(updateDnsSteps);
                    }
                    else
                    {
                        this.LogAnnouncement(
                            Invariant(
                                $"Skipping DNS because the {nameof(ExistingDeploymentStrategy)} was {existingDeploymentStrategy}, can only apply DNS when it is set to {nameof(ExistingDeploymentStrategy.Replace)}."),
                            instanceNumber);
                    }
                }

                await this.RunSetupStepsAsync(machineManager, setupSteps, instanceNumber);
            }

            if ((configToCreateWith.PostDeploymentStrategy ?? new PostDeploymentStrategy()).TurnOffInstance)
            {
                const bool WaitUntilOff = true;
                this.LogAnnouncement("Post deployment strategy: TurnOffInstance is true - shutting down instance.", instanceNumber);
                await this.computingManager.TurnOffInstanceAsync(createdInstanceDescription.Id, createdInstanceDescription.Location, WaitUntilOff);
            }
        }

        private SetupStepBatch[] GetRebootSteps()
        {
            void RebootInstance(IManageMachines machineManager)
            {
                const int MaxFailureCount = 500;
                var failureCount = 0;

                var sleepTimeInSeconds = 10d;
                var rebootCallSucceeded = false;
                while (!rebootCallSucceeded)
                {
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
                        // ignore failures until they exceed threshold...
                        failureCount = failureCount + 1;
                        if (failureCount > MaxFailureCount)
                        {
                            throw new DeploymentException(Invariant($"Failed to reboot: {machineManager.Address}"), ex);
                        }
                    }
                }
            }

            return new[]
                       {
                           new SetupStepBatch
                               {
                                   ExecutionOrder = ExecutionOrder.Reboot,
                                   Steps = new[]
                                               {
                                                   new SetupStep
                                                       {
                                                           Description = "Rebooting instance to finalize installations that require it.",
                                                           SetupFunc = m =>
                                                               {
                                                                   RebootInstance(m);
                                                                   return new object[0];
                                                               },
                                                       },
                                               },
                               },
                       };
        }

        private SetupStepBatch[] GetWaitUntilMachineIsReachableSteps(IManageMachines machineManager)
        {
            return new[]
                       {
                           new SetupStepBatch
                               {
                                   ExecutionOrder = ExecutionOrder.WaitUntilReachable,
                                   Steps = new[]
                                               {
                                                   new SetupStep
                                                       {
                                                           Description = Invariant($"Waiting for machine to be reachable via {machineManager.MachineProtocol} (confirm VPN connection)."),
                                                           SetupFunc = m =>
                                                               {
                                                                   this.WaitUntilMachineIsAccessible(m);
                                                                   return new object[0];
                                                               },
                                                       },
                                               },
                               },
                       };
        }

        private SetupStepBatch[] GetBlockedUntilRebootedSteps()
        {
            return new[]
                       {
                           new SetupStepBatch
                               {
                                   ExecutionOrder = ExecutionOrder.BlockUntilRebooted,
                                   Steps = new[]
                                               {
                                                   new SetupStep
                                                       {
                                                           Description = "Waiting for machine to come back up from reboot.",
                                                           SetupFunc = m =>
                                                               {
                                                                   this.WaitUntilMachineIsAccessible(m);
                                                                   return new object[0];
                                                               },
                                                       },
                                               },
                               },
                       };
        }

        private SetupStepBatch[] GetUpdateDnsSteps(int instanceNumber, string environment, List<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, InstanceDescription createdInstanceDescription, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            List<SetupStep> steps = new List<SetupStep>();

#pragma warning disable SA1305 // Field names should not use Hungarian notation
            SetupStep BuildDnsStep(string dns, string ipAddress, string instanceLocation)
#pragma warning restore SA1305 // Field names should not use Hungarian notation
            {
                var step = new SetupStep
                               {
                                   Description = Invariant($"Pointing {dns} at {ipAddress}."),
                                   SetupFunc = m =>
                                       {
                                           lock (this.syncDnsManager)
                                           {
                                               this.computingManager.UpsertDnsEntryAsync(environment, instanceLocation, dns, new[] { ipAddress }).Wait();
                                           }

                                           return new object[0];
                                       },
                               };
                return step;
            }

            var dnsInitializations = packagedDeploymentConfigsWithDefaultsAndOverrides.GetInitializationStrategiesOf<InitializationStrategyDnsEntry>();
            foreach (var initialization in dnsInitializations)
            {
                if (string.IsNullOrEmpty(initialization.PublicDnsEntry) && string.IsNullOrEmpty(initialization.PrivateDnsEntry))
                {
                    throw new ArgumentException(Invariant($"Instance {instanceNumber} - Cannot create DNS entry of empty or null string, please specify either a public or private dns entry."));
                }

                if (!string.IsNullOrEmpty(initialization.PrivateDnsEntry))
                {
                    var privateIpAddress = createdInstanceDescription.PrivateIpAddress;
                    var privateDnsEntry = funcToCreateNewDnsWithTokensReplaced(initialization.PrivateDnsEntry);

                    var step = BuildDnsStep(privateDnsEntry, privateIpAddress, createdInstanceDescription.Location);
                    steps.Add(step);
                }

                if (!string.IsNullOrEmpty(initialization.PublicDnsEntry))
                {
                    var publicIpAddress = createdInstanceDescription.PublicIpAddress;
                    if (string.IsNullOrEmpty(publicIpAddress))
                    {
                        throw new ArgumentException(
                            Invariant($"Instance {instanceNumber} - Cannot assign a public DNS because there isn't a public IP address on the instance."));
                    }

                    var publicDnsEntry = funcToCreateNewDnsWithTokensReplaced(initialization.PublicDnsEntry);
                    var step = BuildDnsStep(publicDnsEntry, publicIpAddress, createdInstanceDescription.Location);
                    steps.Add(step);
                }
            }

            return new[]
                       {
                           new SetupStepBatch
                               {
                                   ExecutionOrder = ExecutionOrder.Dns,
                                   Steps = steps,
                               },
                       };
        }

        private async Task WaitUntilStatusChecksSucceedAsync(int instanceNumber, InstanceDescription createdInstanceDescription)
        {
            const int MaxFailureCount = 500;
            var failureCount = 0;

            var sleepTimeInSeconds = 10d;
            var allChecksPassed = false;
            while (!allChecksPassed)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                var status = await this.computingManager.GetInstanceStatusAsync(
                    createdInstanceDescription.Id,
                    createdInstanceDescription.Location);

                var statusInstanceChecks = status.InstanceChecks ?? new Dictionary<string, CheckState>();
                var statusSystemChecks = status.SystemChecks ?? new Dictionary<string, CheckState>();

                if (statusInstanceChecks.Any(_ => _.Value == CheckState.Failed))
                {
                    foreach (var instanceCheck in statusInstanceChecks)
                    {
                        this.LogAnnouncement("Instance Check; " + instanceCheck.Key + " = " + instanceCheck.Value, instanceNumber);
                    }

                    throw new DeploymentException("Failure in an instance check.");
                }

                if (statusSystemChecks.Any(_ => _.Value == CheckState.Failed))
                {
                    foreach (var systemCheck in statusSystemChecks)
                    {
                        this.LogAnnouncement("System Check; " + systemCheck.Key + " = " + systemCheck.Value, instanceNumber);
                    }

                    throw new DeploymentException("Failure in a system check.");
                }

                allChecksPassed = statusInstanceChecks.All(_ => _.Value == CheckState.Passed)
                                  && statusSystemChecks.All(_ => _.Value == CheckState.Passed);

                // ignore failures until they exceed threshold...
                failureCount = failureCount + 1;
                if (!allChecksPassed && failureCount > MaxFailureCount)
                {
                    throw new DeploymentException(Invariant($"Status checks never fully passed; InstanceChecks: '{string.Join(",", statusInstanceChecks.Select(_ => _.Key + "=" + _.Value))}', SystemChecks: '{string.Join(",", statusSystemChecks.Select(_ => _.Key + "=" + _.Value))}'."));
                }
            }
        }

        private async Task RunSetupStepsAsync(IManageMachines machineManager, IReadOnlyCollection<SetupStepBatch> setupStepBatches, int instanceNumber)
        {
            var maxTries = this.setupStepFactory.MaxSetupStepAttempts;
            var throwOnFailedSetupStep = this.setupStepFactory.ThrowOnFailedSetupStep;

            this.LogAnnouncement("Running setup steps to finalize setup.", instanceNumber);
            var orderedSteps = setupStepBatches.Where(_ => _ != null).OrderBy(_ => _.ExecutionOrder).SelectMany(_ => _.Steps).Where(_ => _ != null).ToList();
            foreach (var setupStep in orderedSteps)
            {
                Task RetryingSetupAction()
                {
                    return Task.Run(() => this.RunSetupStepWithRetry(machineManager, instanceNumber, setupStep, maxTries, throwOnFailedSetupStep));
                }

                await this.RunActionWithTelemetryAsync("Setup step - " + setupStep.Description, RetryingSetupAction, instanceNumber);
            }
        }

        /// <summary>
        /// Get packages and extract any deployment configuration present.
        /// </summary>
        /// <param name="environment">Environment from Its.Config to use.</param>
        /// <param name="packageManager">Interface to fetch packages from their galleries.</param>
        /// <param name="packageHelper">Helper logic to extract information from packages.</param>
        /// <param name="configFileManager">Interface to manage configuration file creation.</param>
        /// <param name="packagesToDeploy">Packages to deploy.</param>
        /// <returns>List of packages with bytes downloaded and any deployment configuration extracted.</returns>
        public static IReadOnlyCollection<PackagedDeploymentConfiguration> GetPackagedDeploymentConfigurations(
            string environment,
            IGetPackages packageManager,
            PackageHelper packageHelper,
            IManageConfigFiles configFileManager,
            IReadOnlyCollection<PackageDescriptionWithOverrides> packagesToDeploy)
        {
            new { configFileManager }.AsArg().Must().NotBeNull();

            // get deployment details from Its.Config in the package
            var deploymentFileSearchPattern = configFileManager.BuildConfigPath(precedence: environment, fileNameWithExtension: "DeploymentConfigurationWithStrategies.json");

            bool FigureOutIfNeedToBundleDependencies(IHaveInitializationStrategies hasStrategies) =>
                hasStrategies != null && (hasStrategies.GetInitializationStrategiesOf<InitializationStrategyMessageBusHandler>().Any()
                                          || hasStrategies.GetInitializationStrategiesOf<InitializationStrategySqlServer>().Any(_ => _.BundleDependencies));

            var packagedDeploymentConfigs = packagesToDeploy.Select(
                packageDescriptionWithOverrides =>
                    {
                        // decide whether we need to get all of the dependencies or just the normal package
                        // currently this is for message bus handlers since web services already include all assemblies...
                        var bundleAllDependencies = FigureOutIfNeedToBundleDependencies(packageDescriptionWithOverrides);
                        var package = packageHelper.GetPackage(packageDescriptionWithOverrides.PackageDescription, bundleAllDependencies);
                        var actualVersion = packageHelper.GetActualVersionFromPackage(package.Package);

                        var deploymentConfigJson =
                            packageManager.GetMultipleFileContentsFromPackageAsStrings(
                                package.Package,
                                deploymentFileSearchPattern).Select(_ => _.Value).SingleOrDefault();

                        // strip the BOM (Byte Order Mark) since the characters are stored in a string now and encoding should be fine; presence of this will fail many serializers...
                        var deploymentConfigJsonWithoutBom = (deploymentConfigJson ?? string.Empty).Replace("\ufeff", string.Empty);
                        var deploymentConfig = configFileManager.DeserializeConfigFileText<DeploymentConfigurationWithStrategies>(deploymentConfigJsonWithoutBom);

                        // re-check the extracted config to make sure it doesn't change the decision about bundling...
                        var bundleAllDependenciesReCheck = FigureOutIfNeedToBundleDependencies(deploymentConfig);

                        if (!bundleAllDependencies && bundleAllDependenciesReCheck)
                        {
                            // since we previously didn't bundle the packages dependencies
                            //        AND found in the extraced config that we need to
                            //       THEN we need to re-download with dependencies bundled...
                            package = packageHelper.GetPackage(packageDescriptionWithOverrides.PackageDescription, true);
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
                        package.Package.PackageDescription.Version = actualVersion;

                        var newItem = new PackagedDeploymentConfiguration
                                          {
                                              PackageWithBundleIdentifier = package,
                                              DeploymentConfiguration = deploymentConfig,
                                              InitializationStrategies = initializationStrategies,
                                              ItsConfigOverrides = packageDescriptionWithOverrides.ItsConfigOverrides,
                                          };

                        return newItem;
                    }).ToList();

            return packagedDeploymentConfigs;
        }

        private void RunSetupStepWithRetry(
            IManageMachines machineManager,
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
                        var output = setupStep.SetupFunc(machineManager);
                        this.LogDebugAnnouncement("  - " + setupStep.Description, instanceNumber, output);
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
                                Invariant($"Instance {instanceNumber} - Failed to run setup step {setupStep.Description} after {maxTries} attempts"),
                                ex);
                        }
                        else
                        {
                            var exceededMaxTriesMessage = Invariant($"  ! {setupStep.Description} - Exception on try {tries}/{maxTries} - {ex}");
                            this.LogAnnouncement(exceededMaxTriesMessage, instanceNumber);
                        }
                    }
                    else
                    {
                        var failedRetryingMessage = Invariant($"  ! {setupStep.Description} - Exception on try {tries}/{maxTries} - retrying");
                        this.LogAnnouncement(failedRetryingMessage, instanceNumber);
                        this.LogDebugAnnouncement(setupStep.Description, instanceNumber, new[] { ex });
                        Thread.Sleep(TimeSpan.FromSeconds(tries * 10));
                    }
                }
            }
        }

        private IReadOnlyCollection<SetupStepBatch> GetSetupStepsToUpdateArcologyAfterPackageIsDeployed(PackageDescription packageDescription, string instanceId, string environment)
        {
            var ret = new[]
                          {
                              new SetupStepBatch
                                  {
                                      ExecutionOrder = ExecutionOrder.UpdateArcology,
                                      Steps = new[]
                                                  {
                                                      new SetupStep
                                                          {
                                                              Description = Invariant($"Mark deployed - {packageDescription.GetIdDotVersionString()}."),
                                                              SetupFunc = m =>
                                                                  {
                                                                      Run.TaskUntilCompletion(
                                                                          this.tracker.ProcessDeployedPackageAsync(
                                                                              environment,
                                                                              instanceId,
                                                                              packageDescription));
                                                                      return new object[0];
                                                                  },
                                                          },
                                                  },
                                  },
                          };

            return ret;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "notNeededResults", Justification = "Keeping to show there is a result, we just don't need it here.")]
        private void WaitUntilMachineIsAccessible(IManageMachines machineManager)
        {
            var testScript = @"{ ls C:\Windows }";

            const int MaxFailureCount = 500;
            var failureCount = 0;

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
                        var notNeededResults = machineManager.RunScript(testScript);
                    }

                    reachable = true;
                }
                catch (Exception ex)
                {
                    // ignore failures until they exceed threshold...
                    failureCount = failureCount + 1;
                    if (failureCount > MaxFailureCount)
                    {
                        throw new DeploymentException(Invariant($"Failed to execute successful script via WinRM, script = '{testScript}'"), ex);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the password for the instance.
        /// </summary>
        /// <param name="instanceDescription">Instance to find the password for.</param>
        /// <returns>Password for instance.</returns>
        public async Task<string> GetAdminPasswordForInstanceAsync(InstanceDescription instanceDescription)
        {
            const int MaxFailureCount = 500;
            var failureCount = 0;

            string adminPassword = null;
            var sleepTimeInSeconds = 10d;
            var privateKey = await this.tracker.GetPrivateKeyOfInstanceByIdAsync(instanceDescription.Environment, instanceDescription.Id);
            while (adminPassword == null)
            {
                sleepTimeInSeconds = sleepTimeInSeconds * 1.2; // add 20% each loop
                Thread.Sleep(TimeSpan.FromSeconds(sleepTimeInSeconds));

                try
                {
                    adminPassword = await this.computingManager.GetAdministratorPasswordForInstanceAsync(
                        instanceDescription,
                        privateKey);
                }
                catch (PasswordUnavailableException ex)
                {
                    // ignore failures until they exceed threshold...
                    failureCount = failureCount + 1;
                    if (failureCount > MaxFailureCount)
                    {
                        throw new DeploymentException(Invariant($"Failed to retrieve password for '{instanceDescription.Name} | {instanceDescription.PrivateIpAddress}'."), ex);
                    }
                }
            }

            return adminPassword;
        }

        private async Task<string> ProcessExistingDeploymentStrategyAsync(IReadOnlyCollection<PackageDescription> packagesToDeploy, string environment, ExistingDeploymentStrategy existingDeploymentStrategy, string instanceName)
        {
            var packagesToIgnore = this.packageIdsToIgnoreDuringTerminationSearch.Select(_ => new PackageDescription { Id = _ }).ToList();

            // get aws instance object by name (from the AWS object tracking storage)
            var packagesToCheckFor = packagesToDeploy.Except(packagesToIgnore, new PackageDescriptionIdOnlyEqualityComparer()).ToList();
            var instancesMatchingPackagesAllEnvironments =
                (await this.tracker.GetInstancesByDeployedPackagesAsync(environment, packagesToCheckFor)).ToList();
            var instancesWithMatchingEnvironmentAndPackages =
                instancesMatchingPackagesAllEnvironments.Where(
                    _ => string.Equals(_.Environment, environment, StringComparison.CurrentCultureIgnoreCase)).ToList();

            // confirm that terminating the instances will not take down any packages that aren't getting re-deployed...
            var deployedPackagesToCheck =
                instancesWithMatchingEnvironmentAndPackages.SelectMany(_ => _.DeployedPackages.Values.Select(p => p.PackageDescription))
                    .Except(packagesToIgnore, new PackageDescriptionIdOnlyEqualityComparer())
                    .ToList();
            if (deployedPackagesToCheck.Except(packagesToDeploy, new PackageDescriptionIdOnlyEqualityComparer()).Any())
            {
                var deployedIdList = string.Join(",", deployedPackagesToCheck.Select(_ => _.Id));
                var deployingIdList = string.Join(",", packagesToDeploy.Select(_ => _.Id));
                throw new DeploymentException(Invariant($"Cannot proceed because taking down the instances of requested packages will take down packages not getting redeployed => Running: {deployedIdList} Deploying: {deployingIdList}.  If these are expected from usage of {nameof(DeploymentAdjustmentStrategiesApplicator)} then update the exclusion list in {nameof(ComputingInfrastructureManagerSettings)}.{nameof(ComputingInfrastructureManagerSettings.PackageIdsToIgnoreDuringTerminationSearch)}"));
            }

            if (instancesWithMatchingEnvironmentAndPackages.Any())
            {
                switch (existingDeploymentStrategy)
                {
                    case ExistingDeploymentStrategy.Replace:
                        var terminateInstancesTasks =
                            instancesWithMatchingEnvironmentAndPackages.Select(
                                                                            instanceDescription => this.TerminateInstanceAsync(
                                                                                environment,
                                                                                instanceDescription))
                                                                       .ToArray();
                        await Task.WhenAll(terminateInstancesTasks);
                        return instanceName;
                    case ExistingDeploymentStrategy.DeploySideBySide:
                        return instanceName + "-SxS-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmZ", CultureInfo.InvariantCulture);
                    case ExistingDeploymentStrategy.DeployAdditional:
                        throw new NotSupportedException("DeployAdditional is not currently supported.");
                    case ExistingDeploymentStrategy.NotPossibleToReplaceOrDuplicate:
                        throw new ArgumentException(Invariant($"Found existing instances and {nameof(ExistingDeploymentStrategy)} is {existingDeploymentStrategy}, cannot proceed."));
                    default:
                        throw new NotSupportedException(
                            Invariant($"Unsupported {nameof(ExistingDeploymentStrategy)}; {existingDeploymentStrategy}."));
                }
            }
            else
            {
                this.LogAnnouncement("Did not find any existing instances for the specified package list and environment.");
                return instanceName;
            }
        }

        private async Task TerminateInstanceAsync(string environment, InstanceDescription instanceDescription)
        {
            var systemInstanceId = instanceDescription.Id;
            if (string.IsNullOrEmpty(systemInstanceId))
            {
                // this is likely due to a failed previous install - MUST check if the instance actually got created...
                var activeInstancesFromProvider = await this.computingManager.GetActiveInstancesFromProviderAsync(environment);

                var any = activeInstancesFromProvider.SingleOrDefault(_ => _.PrivateIpAddress == instanceDescription.PrivateIpAddress);

                if (any != null)
                {
                    throw new NotSupportedException(
                        "There is an instance in tracking with the package set that is attempting to be deployed that does not have an instance id AND there is an instance with the same private IP address, if this instance is in fact a failed deployment it must be manually terminated, if not then there is probably a configuration issue - Private IP: "
                        + instanceDescription.PrivateIpAddress + ", ID: " + any.Id);
                }
                else
                {
                    this.LogAnnouncement(
                        "Removing a failed prior deployment attempt of packages from tracking because no instance exists with the specified private IP address in the instance provider; IP Address: "
                        + instanceDescription.PrivateIpAddress);
                    await this.tracker.ProcessFailedInstanceDeploymentAsync(environment, instanceDescription.PrivateIpAddress);
                }
            }
            else
            {
                this.LogAnnouncement("Terminating instance => ID: " + systemInstanceId + ", ComputingName: " + instanceDescription.Name);
                await this.computingManager.TerminateInstanceAsync(environment, systemInstanceId, instanceDescription.Location, true);
            }
        }

        private async Task RunActionWithTelemetryAsync(string step, Func<Task> code, int? instanceNumber = null)
        {
            this.StartTelemetry(step, instanceNumber);
            await code();
            this.StopTelemetry(step, instanceNumber);
        }

        private async Task<T> RunFuncWithTelemetryAsync<T>(string step, Func<Task<T>> code, int? instanceNumber = null)
        {
            this.StartTelemetry(step, instanceNumber);
            T ret = await code();
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

            var objectToFlushSerializedString = AnnouncementSerializer.SerializeToString(objectToFlush);

            lock (this.syncTelemetry)
            {
                File.WriteAllText(this.telemetryFile, objectToFlushSerializedString);
            }
        }

        private void LogDebugAnnouncement(string step, int instanceNumber, IReadOnlyCollection<dynamic> output)
        {
            var stringOutput = (output ?? new List<dynamic>()).Select(_ => _ == null ? string.Empty : Convert.ToString(_)).Cast<string>().ToList();

            var instanceNumberAdjustedStep = Invariant($"Instance {instanceNumber} - {step}");
            this.debugAnnouncer(instanceNumberAdjustedStep);
            foreach (var o in stringOutput)
            {
                this.debugAnnouncer(o);
            }

            if (this.debugAnnouncementFile == null)
            {
                return;
            }

            var entry = new DebugAnnouncementEntry
                            {
                                DateTime = DateTime.UtcNow,
                                InstanceNumber = instanceNumber,
                                Output = new[] { instanceNumberAdjustedStep }.Concat(stringOutput).ToArray(),
                            };
            this.debugAnnouncements.Add(entry);
            this.FlushDebugs();
        }

        private void LogAnnouncement(string step, int? instanceNumber = null)
        {
            var instanceNumberAdjustedStep = instanceNumber != null ? "Instance " + instanceNumber + " - " + step : step;
            this.announce(instanceNumberAdjustedStep);

            if (this.announcementFile == null)
            {
                return;
            }

            var entry = new AnnouncementEntry { DateTime = DateTime.UtcNow, InstanceNumber = instanceNumber, Step = step };
            this.announcements.Add(entry);
            this.FlushAnnouncements();
        }

        private void FlushDebugs()
        {
            if (this.debugAnnouncementFile == null)
            {
                return;
            }

            var objectToFlush =
                this.debugAnnouncements.GroupBy(_ => _.InstanceNumber)
                    .OrderBy(_ => _.Key)
                    .Select(instanceGroup => instanceGroup.OrderBy(item => item.DateTime))
                    .SelectMany(_ => _)
                    .ToList();

            var objectToFlushSerializedString = AnnouncementSerializer.SerializeToString(objectToFlush);

            lock (this.syncDebugAnnounmcment)
            {
                File.WriteAllText(this.debugAnnouncementFile, objectToFlushSerializedString);
            }
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

            var objectToFlushSerializedString = AnnouncementSerializer.SerializeToString(objectToFlush);

            lock (this.syncAnnounmcment)
            {
                File.WriteAllText(this.announcementFile, objectToFlushSerializedString);
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
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Keep for serialization.")]
            public string Step { get; set; }

            public DateTime? DateTime { get; set; }
        }

        private class DebugAnnouncementEntry
        {
            public int? InstanceNumber { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local - want this for serialization...
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Keep for serialization.")]
            public string[] Output { get; set; }

            public DateTime? DateTime { get; set; }
        }
    }
}
