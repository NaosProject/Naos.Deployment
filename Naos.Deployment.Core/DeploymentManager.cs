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
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;
    using Naos.MessageBus.Domain;
    using Naos.Packaging.Domain;
    using Naos.WinRM;

    using Newtonsoft.Json;

    using Polly;

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

        private readonly IManageComputingInfrastructure computingManager;

        private readonly IGetPackages packageManager;

        private readonly DeploymentConfiguration defaultDeploymentConfiguration;

        private readonly Action<string> announce;

        private readonly Action<string> debugAnnouncer;

        private readonly string workingDirectory;

        private readonly SetupStepFactory setupStepFactory;

        private readonly MessageBusConnectionConfiguration messageBusPersistenceConnectionConfiguration;

        private readonly ICollection<string> packageIdsToIgnoreDuringTerminationSearch;

        private readonly MessageBusHandlerHarnessConfiguration messageBusHandlerHarnessConfiguration;

        private readonly string[] itsConfigPrecedenceAfterEnvironment;

        private readonly ConcurrentBag<TelemetryEntry> telemetry = new ConcurrentBag<TelemetryEntry>();

        private readonly string telemetryFile;

        private readonly ConcurrentBag<AnnouncementEntry> announcements = new ConcurrentBag<AnnouncementEntry>();

        private readonly string announcementFile;

        private readonly ConcurrentBag<DebugAnnouncementEntry> debugAnnouncements = new ConcurrentBag<DebugAnnouncementEntry>();

        private readonly string debugAnnouncementFile;

        private readonly PackageHelper packageHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentManager"/> class.
        /// </summary>
        /// <param name="tracker">Tracker of computing infrastructure.</param>
        /// <param name="computingManager">Manager of the computing infrastructure (wraps custom computing interactions).</param>
        /// <param name="packageManager">Proxy to retrieve packages.</param>
        /// <param name="certificateRetriever">Manager of certificates (get passwords and file bytes by name).</param>
        /// <param name="defaultDeploymentConfiguration">Default deployment configuration to substitute the values for any nulls.</param>
        /// <param name="messageBusHandlerHarnessConfiguration">Settings and description of the harness to use for message bus handler initializations.</param>
        /// <param name="setupStepFactorySettings">Settings for the setup step factory.</param>
        /// <param name="messageBusPersistenceConnectionConfiguration">Connection string to the message bus harness.</param>
        /// <param name="packageIdsToIgnoreDuringTerminationSearch">List of package IDs to exclude during replacement search.</param>
        /// <param name="announcer">Callback to get status messages through process.</param>
        /// <param name="debugAnnouncer">Callback to get more detailed information through process.</param>
        /// <param name="workingDirectory">Directory to perform temp disk operations.</param>
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
            MessageBusHandlerHarnessConfiguration messageBusHandlerHarnessConfiguration,
            SetupStepFactorySettings setupStepFactorySettings,
            MessageBusConnectionConfiguration messageBusPersistenceConnectionConfiguration,
            ICollection<string> packageIdsToIgnoreDuringTerminationSearch,
            Action<string> announcer, 
            Action<string> debugAnnouncer, 
            string workingDirectory,
            string environmentCertificateName = null,
            string announcementFile = null, 
            string debugAnnouncementFile = null, 
            string telemetryFile = null)
        {
            this.tracker = tracker;
            this.computingManager = computingManager;
            this.packageManager = packageManager;
            this.defaultDeploymentConfiguration = defaultDeploymentConfiguration;
            this.messageBusPersistenceConnectionConfiguration = messageBusPersistenceConnectionConfiguration;
            this.messageBusHandlerHarnessConfiguration = messageBusHandlerHarnessConfiguration;
            this.packageIdsToIgnoreDuringTerminationSearch = packageIdsToIgnoreDuringTerminationSearch;
            this.announce = announcer;
            this.debugAnnouncer = debugAnnouncer;
            this.workingDirectory = workingDirectory;
            this.telemetryFile = telemetryFile;
            this.announcementFile = announcementFile;
            this.debugAnnouncementFile = debugAnnouncementFile;
            this.itsConfigPrecedenceAfterEnvironment = new[] { "Common" };
            this.setupStepFactory = new SetupStepFactory(
                setupStepFactorySettings,
                certificateRetriever,
                packageManager,
                this.itsConfigPrecedenceAfterEnvironment,
                environmentCertificateName);

            this.packageHelper = new PackageHelper(packageManager, this.setupStepFactory.RootPackageDirectoriesToPrune, this.workingDirectory);
        }

        /// <inheritdoc />
        public async Task DeployPackagesAsync(ICollection<PackageDescriptionWithOverrides> packagesToDeploy, string environment, string instanceName, DeploymentConfiguration deploymentConfigOverride = null)
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
            var deploymentFileSearchPattern = $".config/{environment}/DeploymentConfigurationWithStrategies.json";

            this.LogAnnouncement("Extracting deployment configuration(s) for specified environment from packages (if present).");
            var packagedDeploymentConfigs = this.GetPackagedDeploymentConfigurations(packagesToDeploy, deploymentFileSearchPattern);
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

            // terminate existing instances...
            await
                this.RunActionWithTelemetryAsync(
                    "Terminating Instances",
                    () => this.TerminateInstancesBeingReplacedAsync(packagesToDeploy.WithoutStrategies(), environment));

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
                        instanceNumber,
                        packagesToDeploy,
                        environment,
                        instanceName,
                        instanceCount,
                        configToCreateWith,
                        packagedDeploymentConfigsWithDefaultsAndOverrides)).ToArray();

            await Task.WhenAll(tasks);

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
                this.computingManager.CreateNewInstanceAsync(
                    environment,
                    numberedInstanceName,
                    configToCreateWith,
                    packagesToDeploy.Select(_ => _ as PackageDescription).ToList(),
                    configToCreateWith.DeploymentStrategy.IncludeInstanceInitializationScript);

            var systemSpecificDetailsAsString = string.Join(
                ",",
                createdInstanceDescription.SystemSpecificDetails.Select(_ => _.Key + "=" + _.Value).ToArray());
            var createdInstanceMessage =
                $"Instance {instanceNumber} - Created new instance => ComputingName: {createdInstanceDescription.Name}, ID: {createdInstanceDescription.Id}, Private IP: {createdInstanceDescription.PrivateIpAddress}, System Specific Details: {systemSpecificDetailsAsString}";
            this.LogAnnouncement(createdInstanceMessage, instanceNumber);

            this.LogAnnouncement("Waiting for status checks to pass.", instanceNumber);
            await this.WaitUntilStatusChecksSucceedAsync(instanceNumber, createdInstanceDescription);

            Func<string, string> funcToCreateNewDnsWithTokensReplaced = tokenizedDns => TokenSubstitutions.GetSubstitutedStringForDns(tokenizedDns, environment, numberedInstanceName, instanceNumber);

            if (configToCreateWith.DeploymentStrategy.RunSetupSteps)
            {
                this.LogAnnouncement("Waiting for Administrator password to be available (takes a few minutes for this).", instanceNumber);

                var adminPasswordClear =
                    await
                    this.RunFuncWithTelemetryAsync(
                        "Wait for admin password",
                        () => this.GetAdminPasswordForInstanceAsync(createdInstanceDescription),
                        instanceNumber);

                var machineManager = new MachineManager(
                    createdInstanceDescription.PrivateIpAddress,
                    this.setupStepFactory.AdministratorAccount,
                    MachineManager.ConvertStringToSecureString(adminPasswordClear),
                    true);

                this.LogAnnouncement("Waiting for machine to be accessible via WinRM (requires connectivity - make sure VPN is up if applicable).", instanceNumber);
                await
                    this.RunActionWithTelemetryAsync(
                        "Wait for WinRM access",
                        () => Task.Run(() => this.WaitUntilMachineIsAccessible(machineManager)),
                        instanceNumber);

                var instanceLevelSetupSteps =
                    await
                    this.setupStepFactory.GetInstanceLevelSetupSteps(
                        createdInstanceDescription.ComputerName,
                        configToCreateWith.InstanceType.WindowsSku,
                        environment,
                        configToCreateWith.ChocolateyPackages,
                        packagedDeploymentConfigsWithDefaultsAndOverrides.SelectMany(_ => _.InitializationStrategies).ToList());

                this.LogAnnouncement("Running setup actions that finalize the instance creation.", instanceNumber);
                await this.RunActionWithTelemetryAsync("Run instance level setup steps", () => this.RunSetupStepsAsync(machineManager, instanceLevelSetupSteps, instanceNumber), instanceNumber);

                // this is necessary for finishing start up items, might have to try a few times until WinRM is available...
                this.LogAnnouncement("Rebooting new instance to finalize any items from instance setup.", instanceNumber);
                this.RebootInstance(instanceNumber, machineManager);

                // determine if a message bus harness package should be included
                var harnessPackagedConfig = this.GetMessageBusHarnessPackagedDeploymentConfigurationIfNecessary(
                    environment,
                    instanceName,
                    instanceNumber,
                    packagedDeploymentConfigsWithDefaultsAndOverrides,
                    configToCreateWith);

                // soft clone the list b/c we don't want to mess up the collection for other threads running different instance numbers
                var packagedDeploymentConfigsWithDefaultsAndOverridesAndHarness = packagedDeploymentConfigsWithDefaultsAndOverrides.Select(_ => _).ToList();

                if (harnessPackagedConfig != null)
                {
                    this.LogAnnouncement("Configured message bus harness config included in deployed package list");
                    packagedDeploymentConfigsWithDefaultsAndOverridesAndHarness.Add(harnessPackagedConfig);
                }

                foreach (var packagedConfig in packagedDeploymentConfigsWithDefaultsAndOverridesAndHarness)
                {
                    var setupSteps = await this.setupStepFactory.GetSetupStepsAsync(packagedConfig, environment, adminPasswordClear, funcToCreateNewDnsWithTokensReplaced);
                    this.LogAnnouncement("Running setup actions for package: " + packagedConfig.PackageWithBundleIdentifier.Package.PackageDescription.GetIdDotVersionString(), instanceNumber);

                    await this.RunSetupStepsAsync(machineManager, setupSteps, instanceNumber);

                    // Mark the instance as having the successfully deployed packages
                    await this.tracker.ProcessDeployedPackageAsync(
                        environment,
                        createdInstanceDescription.Id,
                        packagedConfig.PackageWithBundleIdentifier.Package.PackageDescription);
                }

                this.UpsertDnsEntriesAsNecessary(instanceNumber, environment, packagedDeploymentConfigsWithDefaultsAndOverridesAndHarness, createdInstanceDescription, funcToCreateNewDnsWithTokensReplaced);
            }

            if ((configToCreateWith.PostDeploymentStrategy ?? new PostDeploymentStrategy()).TurnOffInstance)
            {
                const bool WaitUntilOff = true;
                this.LogAnnouncement("Post deployment strategy: TurnOffInstance is true - shutting down instance.", instanceNumber);
                await this.computingManager.TurnOffInstanceAsync(
                    createdInstanceDescription.Id,
                    createdInstanceDescription.Location,
                    // ReSharper disable once RedundantArgumentDefaultValue - keeping for clarity of what's happening...
                    WaitUntilOff);
            }
        }

        private void UpsertDnsEntriesAsNecessary(
            int instanceNumber,
            string environment,
            ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides,
            InstanceDescription createdInstanceDescription,
            Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            // get all DNS initializations to update any private DNS entries.
            this.LogAnnouncement("Updating DNS for all DNS initializations (if applicable)", instanceNumber);
            var dnsInitializations = packagedDeploymentConfigsWithDefaultsAndOverrides.GetInitializationStrategiesOf<InitializationStrategyDnsEntry>();
            foreach (var initialization in dnsInitializations)
            {
                if (string.IsNullOrEmpty(initialization.PublicDnsEntry) && string.IsNullOrEmpty(initialization.PrivateDnsEntry))
                {
                    throw new ArgumentException(
                        "Instance " + instanceNumber + " - Cannot create DNS entry of empty or null string, please specify either a public or private dns entry.");
                }

                if (!string.IsNullOrEmpty(initialization.PrivateDnsEntry))
                {
                    var privateIpAddress = createdInstanceDescription.PrivateIpAddress;
                    var privateDnsEntry = funcToCreateNewDnsWithTokensReplaced(initialization.PrivateDnsEntry);

                    this.UpsertDnsEntry(instanceNumber, environment, privateDnsEntry, privateIpAddress, createdInstanceDescription.Location);
                }

                if (!string.IsNullOrEmpty(initialization.PublicDnsEntry))
                {
                    var publicIpAddress = createdInstanceDescription.PublicIpAddress;
                    if (string.IsNullOrEmpty(publicIpAddress))
                    {
                        throw new ArgumentException(
                            "Instance " + instanceNumber + " - Cannot assign a public DNS because there isn't a public IP address on the instance.");
                    }

                    var publicDnsEntry = funcToCreateNewDnsWithTokensReplaced(initialization.PublicDnsEntry);

                    this.UpsertDnsEntry(instanceNumber, environment, publicDnsEntry, publicIpAddress, createdInstanceDescription.Location);
                }
            }
        }

        private void UpsertDnsEntry(int instanceNumber, string environment, string dns, string ipAddress, string instanceLocation)
        {
            this.LogAnnouncement($" - Pointing {dns} at {ipAddress}.", instanceNumber);

            lock (this.syncDnsManager)
            {
                this.computingManager.UpsertDnsEntryAsync(environment, instanceLocation, dns, new[] { ipAddress }).Wait();
            }
        }

        private PackagedDeploymentConfiguration GetMessageBusHarnessPackagedDeploymentConfigurationIfNecessary(
            string environment,
            string instanceName, 
            int instanceNumber, 
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
                    _ => _.PackageWithBundleIdentifier.Package.PackageDescription.Id == this.messageBusHandlerHarnessConfiguration.Package.Id);

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

                    var packageFolderName = packageWithMessageBusInitializations.PackageWithBundleIdentifier.Package.PackageDescription.GetIdDotVersionString();

                    // extract appropriate files from 
                    var itsConfigFilesFromPackage = new Dictionary<string, string>();
                    var precedenceChain = new[] { environment }.ToList();
                    precedenceChain.AddRange(this.itsConfigPrecedenceAfterEnvironment);
                    foreach (var precedenceElement in precedenceChain)
                    {
                        var itsConfigFolderPattern = packageWithMessageBusInitializations.PackageWithBundleIdentifier.AreDependenciesBundled
                                                         ? $"{packageFolderName}/Configuration/.config/{precedenceElement}/"
                                                         : $".config/{precedenceElement}/";

                        var itsConfigFilesFromPackageForPrecedenceElement =
                            this.packageManager.GetMultipleFileContentsFromPackageAsStrings(
                                packageWithMessageBusInitializations.PackageWithBundleIdentifier.Package,
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
                    environment,
                    instanceName,
                    instanceNumber,
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

                var status = await this.computingManager.GetInstanceStatusAsync(
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

        private async Task RunSetupStepsAsync(MachineManager machineManager, ICollection<SetupStep> setupSteps, int instanceNumber)
        {
            var maxTries = this.setupStepFactory.MaxSetupStepAttempts;
            var throwOnFailedSetupStep = this.setupStepFactory.ThrowOnFailedSetupStep;

            foreach (var setupStep in setupSteps)
            {
                Func<Task> retryingSetupAction = () =>
                    {
                        return Task.Run(
                            () =>
                            this.RunSetupStepWithRetry(
                                machineManager,
                                instanceNumber,
                                setupStep,
                                maxTries,
                                throwOnFailedSetupStep));
                    };

                await this.RunActionWithTelemetryAsync("Setup step - " + setupStep.Description, retryingSetupAction, instanceNumber);
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
                        var package = this.packageHelper.GetPackage(packageDescriptionWithOverrides, bundleAllDependencies);
                        var actualVersion = this.GetActualVersionFromPackage(package.Package);

                        var deploymentConfigJson =
                            this.packageManager.GetMultipleFileContentsFromPackageAsStrings(
                                package.Package,
                                deploymentFileSearchPattern).Select(_ => _.Value).SingleOrDefault();
                        var deploymentConfig =
                            (deploymentConfigJson ?? string.Empty).Replace("\ufeff", string.Empty).FromJson<DeploymentConfigurationWithStrategies>(); // strip the BOM as it makes Newtonsoft bomb...

                        // re-check the extracted config to make sure it doesn't change the decision about bundling...
                        var bundleAllDependenciesReCheck = figureOutIfNeedToBundleDependencies(deploymentConfig);

                        if (!bundleAllDependencies && bundleAllDependenciesReCheck)
                        {
                            // since we previously didn't bundle the packages dependencies 
                            //        AND found in the extraced config that we need to 
                            //       THEN we need to re-download with dependencies bundled...
                            package = this.packageHelper.GetPackage(packageDescriptionWithOverrides, true);
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
                                $"Instance {instanceNumber} - Failed to run setup step {setupStep.Description} after {maxTries} attempts",
                                ex);
                        }
                        else
                        {
                            var exceededMaxTriesMessage = $"Step {setupStep.Description} - Exception on try {tries}/{maxTries} - {ex}";
                            this.LogAnnouncement(exceededMaxTriesMessage, instanceNumber);
                        }
                    }
                    else
                    {
                        var failedRetryingMessage = $"Step {setupStep.Description} - Exception on try {tries}/{maxTries} - retrying";
                        this.LogAnnouncement(failedRetryingMessage, instanceNumber);
                        this.LogDebugAnnouncement(setupStep.Description, instanceNumber, new[] { ex });
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

        private PackagedDeploymentConfiguration BuildMessageBusHarnessPackagedConfig(
            string environment,
            string instanceName,
            int instanceNumber,
            ICollection<InitializationStrategyMessageBusHandler> messageBusInitializations, 
            ICollection<ItsConfigOverride> itsConfigOverrides, 
            DeploymentConfiguration configToCreateWith)
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

            var messageBusHandlerPackage = this.packageHelper.GetPackage(this.messageBusHandlerHarnessConfiguration.Package, false);

            var actualVersion = this.GetActualVersionFromPackage(messageBusHandlerPackage.Package);
            messageBusHandlerPackage.Package.PackageDescription.Version = actualVersion;

            var channelsToMonitor = messageBusInitializations.SelectMany(_ => _.ChannelsToMonitor).Distinct().ToList();

            // recreate channels with channel name substitutions
            var simpleChannels = channelsToMonitor.OfType<SimpleChannel>().ToList();
            if (simpleChannels.Count != channelsToMonitor.Count)
            {
                throw new ArgumentException("There are IChannels that are NOT SimpleChannels and this is not supported for the token substitution.");
            }

            var adjustedChannelsToMonitor =
                simpleChannels
                    .Select(_ => new SimpleChannel(TokenSubstitutions.GetSubstitutedStringForChannelName(_.Name, environment, instanceName, instanceNumber)))
                    .Cast<IChannel>()
                    .ToList();

            var workerCount = messageBusInitializations.Max(_ => _.WorkerCount);
            workerCount = workerCount == 0 ? 1 : workerCount;

            var executorRoleSettings = new[]
                                           {
                                               new MessageBusHarnessRoleSettingsExecutor
                                                   {
                                                       ChannelsToMonitor =
                                                           adjustedChannelsToMonitor,
                                                       HandlerAssemblyPath =
                                                           this.setupStepFactory
                                                           .RootDeploymentPath,
                                                       WorkerCount = workerCount,
                                                       PollingTimeSpan =
                                                           TimeSpan.FromMinutes(1),
                                                       TypeMatchStrategy = TypeMatchStrategy.NamespaceAndName,
                                                       MessageDispatcherWaitThreadSleepTime = TimeSpan.FromSeconds(.5),
                                                       RetryCount = 0,
                                                       HarnessProcessTimeToLive = this.messageBusHandlerHarnessConfiguration.HandlerHarnessProcessTimeToLive
                                               }
                                           };

            var messageBusHandlerSettings = new MessageBusHarnessSettings
                                                {
                                                    ConnectionConfiguration = this.messageBusPersistenceConnectionConfiguration,
                                                    RoleSettings = executorRoleSettings,
                                                    LogProcessorSettings =
                                                        this.messageBusHandlerHarnessConfiguration.LogProcessorSettings
                                                };

            // add the override that will activate the harness in executor mode.
            var messageBusHandlerSettingsJson = messageBusHandlerSettings.ToJson();
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
                                                PackageWithBundleIdentifier = messageBusHandlerPackage,
                                                ItsConfigOverrides =
                                                    itsConfigOverridesToUse,
                                                InitializationStrategies =
                                                    messageBusHandlerHarnessInitializationStrategies,
                                            };

            return harnessPackagedConfig;
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
            const int MaxExceptionCount = 10000;
            var exceptionCount = 0;

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
                    // swallow exceptions on purpose unless they exceed threshold...
                    exceptionCount = exceptionCount + 1;
                    if (exceptionCount > MaxExceptionCount)
                    {
                        throw;
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
                catch (PasswordUnavailableException)
                {
                    // No-op - just wait until it is available
                }
            }

            return adminPassword;
        }

        private async Task TerminateInstancesBeingReplacedAsync(ICollection<PackageDescription> packagesToDeploy, string environment)
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
                        instanceDescription => this.TerminateInstanceAsync(environment, instanceDescription)).ToArray();

                await Task.WhenAll(tasks);
            }
            else
            {
                this.LogAnnouncement("Did not find any existing instances for the specified package list and environment.");
            }
        }

        private async Task TerminateInstanceAsync(string environment, InstanceDescription instanceDescription)
        {
            var systemInstanceId = instanceDescription.Id;
            if (string.IsNullOrEmpty(systemInstanceId))
            {
                // this is likely due to a failed previous install - MUST check if the instance actually got created...
                var activeInstancesFromProvider = await this.computingManager.GetActiveInstancesFromProviderAsync(environment, instanceDescription.Location);

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

        private void RunWithRetry(Action action, int retryCount = 3)
        {
            Policy.Handle<Exception>().WaitAndRetry(retryCount, attempt => TimeSpan.FromSeconds(attempt * 5)).Execute(action);
        }

        private T RunWithRetry<T>(Func<T> func, int retryCount = 3)
        {
            return Policy.Handle<Exception>().WaitAndRetry(retryCount, attempt => TimeSpan.FromSeconds(attempt * 5)).Execute(func);
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

            var json = JsonConvert.SerializeObject(objectToFlush);

            lock (this.telemetryFile)
            {
                File.WriteAllText(this.telemetryFile, json);
            }
        }

        private void LogDebugAnnouncement(string step, int instanceNumber, ICollection<dynamic> output)
        {
            var stringOutput = (output ?? new List<dynamic>()).Select(_ => _ == null ? string.Empty : Convert.ToString(_)).Cast<string>().ToList();

            var instanceNumberAdjustedStep = "Instance " + instanceNumber + " - " + step;
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
                                Output = new[] { instanceNumberAdjustedStep }.Union(stringOutput).ToArray()
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

            var json = JsonConvert.SerializeObject(objectToFlush);

            lock (this.debugAnnouncementFile)
            {
                File.WriteAllText(this.debugAnnouncementFile, json);
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

        private class DebugAnnouncementEntry 
        {
            public int? InstanceNumber { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local - want this for serialization...
            public string[] Output { get; set; }

            public DateTime? DateTime { get; set; }
        }
    }
}
