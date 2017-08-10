// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Deployer.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using AsyncBridge;
    using CLAP;
    using Its.Configuration;
    using Its.Log.Instrumentation;
    using Naos.AWS.Contract;
    using Naos.Deployment.ComputingManagement;
    using Naos.Deployment.Core;
    using Naos.Deployment.Domain;
    using Naos.Deployment.Tracking;
    using Naos.MessageBus.Domain;
    using Naos.Packaging.Domain;
    using Naos.Recipes.Configuration.Setup;
    using OBeautifulCode.Collection;
    using Spritely.Recipes;
    using Spritely.Redo;
    using static System.FormattableString;

    /// <summary>
    /// Deployment logic to be invoked from the console harness.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors", Justification = "Used by CLAP.")]
    public class Deployer
    {
        private static readonly object NugetAnnouncementFileLock = new object();

        /// <summary>
        /// Gets new credentials on the computing platform provider, will be prepped such that output can be saved to a variable and passed back in for CredentialsJson parameter.
        /// </summary>
        /// <param name="location">Computing platform provider location to make the call against.</param>
        /// <param name="tokenLifespan">Life span of the credentials (in format dd:hh:mm).</param>
        /// <param name="username">Username of the credentials.</param>
        /// <param name="password">Password of the credentials.</param>
        /// <param name="virtualMfaDeviceId">Virtual MFA device id of the credentials.</param>
        /// <param name="mfaValue">Token from the MFA device to use when authenticating.</param>
        /// <param name="startDebugger">A value indicating whether or not to launch the debugger.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "username", Justification = "Not sure why it's complaining...")]
        [Verb(Aliases = "credentials", Description = "Gets new credentials on the computing platform provider, will be prepped such that output can be saved to a variable and passed back in for CredentialsJson parameter.")]
        public static void GetNewCredentialJson(
            [Aliases("")] [Description("Computing platform provider location to make the call against.")] string location,
            [Aliases("")] [Description("Life span of the credentials (in format dd:hh:mm).")] string tokenLifespan,
            [Aliases("")] [Description("Username of the credentials.")] string username,
            [Aliases("")] [Description("Password of the credentials.")] string password,
            [Aliases("")] [Description("Virtual MFA device id of the credentials.")] string virtualMfaDeviceId,
            [Aliases("")] [Description("Token from the MFA device to use when authenticating.")] string mfaValue,
            [Aliases("")] [Description("Start the debugger.")] [DefaultValue(false)] bool startDebugger)
        {
            if (startDebugger)
            {
                Debugger.Launch();
            }

            var tokenLifespanTimeSpan = GetTimeSpanFromDayHourMinuteColonDelimited(tokenLifespan);
            var retObj = ComputingInfrastructureManagerForAws.GetNewCredentials(
                location,
                tokenLifespanTimeSpan,
                username,
                password,
                virtualMfaDeviceId,
                mfaValue);

            var rawRet = retObj.ToJson();

            // prep to be returned in a way that can be piped to a variable and then passed back in...
            var withoutNewLines = rawRet.Replace(Environment.NewLine, string.Empty);
            var escapedQuotes = withoutNewLines.Replace("\"", "\\\"");

            var ret = escapedQuotes;
            Console.Write(ret);
        }

        /// <summary>
        /// Deploys a new instance with specified packages.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="nugetPackageRepositoryConfigurationJson">NuGet Repository/Gallery configuration.</param>
        /// <param name="certificateRetrieverJson">Certificate retriever configuration JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="overrideDeploymentConfigJson">Optional deployment configuration to use as an override in JSON.</param>
        /// <param name="environmentCertificateName">Optional certificate name for an environment certificate saved in certificate manager being configured.</param>
        /// <param name="announcementFilePath">Optional announcement file path to write a JSON file of announcements (will overwrite if existing).</param>
        /// <param name="debugAnnouncementFilePath">Optional announcement file path to write a JSON file of debug announcements (will overwrite if existing)</param>
        /// <param name="telemetryFilePath">Optional telemetry file path to write a JSON file of certain step timings (will overwrite if existing).</param>
        /// <param name="nugetAnnouncementFilePath">Optional nuget file path to write a JSON file of output from nuget (will overwrite if existing).</param>
        /// <param name="environment">Environment to deploy to.</param>
        /// <param name="instanceName">Optional name of the instance (one will be generated from the package list if not provided).</param>
        /// <param name="workingPath">Optional working directory for packages (default will be Temp Dir but might result in PathTooLongException).</param>
        /// <param name="packagesToDeployJson">Optional packages descriptions (with overrides) to configure the instance with.</param>
        /// <param name="deploymentAdjustmentApplicatorJson">Optional deployment adjustment strategies to use.</param>
        /// <param name="startDebugger">A value indicating whether or not to launch the debugger.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "telemetryFilePath", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "nugetAnnouncementFilePath", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "debugAnnouncementFilePath", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "announcementFilePath", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "environmentCertificateName", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "packagesToDeployJson", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "instanceName", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "overrideDeploymentConfigJson", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "infrastructureTrackerJson", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "certificateRetrieverJson", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "nugetPackageRepositoryConfigurationJson", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "deploymentAdjustmentApplicatorJson", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "credentialsJson", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "workingPath", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "nuget", Justification = "Not sure why it's complaining...")]
        [Verb(Aliases = "deploy", Description = "Deploys a new instance with specified packages.")]
        public static void Deploy(
            [Aliases("")] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Description("NuGet Repository/Gallery configuration.")] string nugetPackageRepositoryConfigurationJson,
            [Aliases("")] [Description("Certificate retriever configuration JSON.")] string certificateRetrieverJson,
            [Aliases("")] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Description("Optional deployment configuration to use as an override in JSON.")] [DefaultValue(null)] string overrideDeploymentConfigJson,
            [Aliases("")] [Description("Optional certificate name for an environment certificate saved in certificate manager being configured.")] [DefaultValue(null)] string environmentCertificateName,
            [Aliases("")] [Description("Optional announcement file path to write a JSON file of announcements (will overwrite if existing).")] [DefaultValue(null)] string announcementFilePath,
            [Aliases("")] [Description("Optional announcement file path to write a JSON file of debug announcements (will overwrite if existing).")] [DefaultValue(null)] string debugAnnouncementFilePath,
            [Aliases("")] [Description("Optional telemetry file path to write a JSON file of certain step timings (will overwrite if existing).")] [DefaultValue(null)] string telemetryFilePath,
            [Aliases("")] [Description("Optional nuget file path to write a JSON file of output from nuget (will overwrite if existing).")] [DefaultValue(null)] string nugetAnnouncementFilePath,
            [Aliases("")] [Description("Environment to deploy to.")] string environment,
            [Aliases("")] [Description("Optional name of the instance (one will be generated from the package list if not provided).")] [DefaultValue(null)] string instanceName,
            [Aliases("")] [Description("Optional working directory for packages (default will be Temp Dir but might result in PathTooLongException).")] [DefaultValue(null)] string workingPath,
            [Aliases("")] [Description("Optional packages descriptions (with overrides) to configure the instance with.")] [DefaultValue("[]")] string packagesToDeployJson,
            [Aliases("")] [Description("Optional deployment adjustment strategies to use.")] [DefaultValue("[]")] string deploymentAdjustmentApplicatorJson,
            [Aliases("")] [Description("Start the debugger.")] [DefaultValue(false)] bool startDebugger)
        {
            if (startDebugger)
            {
                Debugger.Launch();
            }

            WriteAsciiArt();

            Console.WriteLine("PARAMETERS:");
            Console.WriteLine("--                                       workingPath: " + workingPath);
            Console.WriteLine("--                                   credentialsJson: " + credentialsJson);
            Console.WriteLine("--                deploymentAdjustmentApplicatorJson: " + deploymentAdjustmentApplicatorJson);
            Console.WriteLine("--           nugetPackageRepositoryConfigurationJson: " + nugetPackageRepositoryConfigurationJson);
            Console.WriteLine("--                          certificateRetrieverJson: " + certificateRetrieverJson);
            Console.WriteLine("--                         infrastructureTrackerJson: " + infrastructureTrackerJson);
            Console.WriteLine("--                      overrideDeploymentConfigJson: " + overrideDeploymentConfigJson);
            Console.WriteLine("--                                       environment: " + environment);
            Console.WriteLine("--                                      instanceName: " + instanceName);
            Console.WriteLine("--                              packagesToDeployJson: " + packagesToDeployJson);
            Console.WriteLine("--                        environmentCertificateName: " + environmentCertificateName);
            Console.WriteLine("--                              announcementFilePath: " + announcementFilePath);
            Console.WriteLine("--                         debugAnnouncementFilePath: " + debugAnnouncementFilePath);
            Console.WriteLine("--                         nugetAnnouncementFilePath: " + nugetAnnouncementFilePath);
            Console.WriteLine("--                                 telemetryFilePath: " + telemetryFilePath);
            Console.WriteLine(string.Empty);

            Config.SetupSerialization();

            var packagesToDeploy = packagesToDeployJson.FromJson<ICollection<PackageDescriptionWithOverrides>>();
            var certificateRetrieverConfiguration = certificateRetrieverJson.FromJson<CertificateManagementConfigurationBase>();
            var infrastructureTrackerConfiguration = infrastructureTrackerJson.FromJson<InfrastructureTrackerConfigurationBase>();
            var deploymentAdjustmentStrategiesApplicator = deploymentAdjustmentApplicatorJson.FromJson<DeploymentAdjustmentStrategiesApplicator>();

            var setupFactorySettings = Settings.Get<SetupStepFactorySettings>();
            var computingInfrastructureManagerSettings = Settings.Get<ComputingInfrastructureManagerSettings>();
            var defaultDeploymentConfiguration = Settings.Get<DefaultDeploymentConfiguration>();

            var certificateRetriever = CertificateManagementFactory.CreateReader(certificateRetrieverConfiguration);
            var credentials = credentialsJson.FromJson<CredentialContainer>();
            using (var infrastructureTracker = InfrastructureTrackerFactory.Create(infrastructureTrackerConfiguration))
            {
                using (var computingManager = new ComputingInfrastructureManagerForAws(computingInfrastructureManagerSettings, infrastructureTracker))
                {
                    computingManager.InitializeCredentials(credentials);
                    var tempDir = Path.GetTempPath();
                    var unzipDirPath = Path.Combine(tempDir, "Naos.Deployment.Temp");
                    if (!string.IsNullOrEmpty(workingPath))
                    {
                        unzipDirPath = workingPath;
                    }

                    if (Directory.Exists(unzipDirPath))
                    {
                        Using.LinearBackOff(TimeSpan.FromSeconds(5))
                            .WithMaxRetries(3)
                            .WithReporter(_ => Log.Write(new LogEntry(Invariant($"Retrying delete deployment working directory {unzipDirPath} due to error."), _)))
                            .Run(() => Directory.Delete(unzipDirPath, true))
                            .Now();
                    }

                    Using.LinearBackOff(TimeSpan.FromSeconds(5))
                        .WithMaxRetries(3)
                        .WithReporter(_ => Log.Write(new LogEntry(Invariant($"Retrying create deployment working directory {unzipDirPath} due to error."), _)))
                        .Run(() => Directory.CreateDirectory(unzipDirPath))
                        .Now();

                    var repoConfig = nugetPackageRepositoryConfigurationJson.FromJson<PackageRepositoryConfiguration>();

                    if (File.Exists(nugetAnnouncementFilePath))
                    {
                        File.Delete(nugetAnnouncementFilePath);
                    }

                    using (var packageManager = PackageRetrieverFactory.BuildPackageRetriever(repoConfig, unzipDirPath, s => NugetAnnouncementAction(s, nugetAnnouncementFilePath)))
                    {
                        var deploymentManager = new DeploymentManager(
                                                    infrastructureTracker,
                                                    computingManager,
                                                    packageManager,
                                                    certificateRetriever,
                                                    defaultDeploymentConfiguration,
                                                    setupFactorySettings,
                                                    deploymentAdjustmentStrategiesApplicator,
                                                    computingInfrastructureManagerSettings.PackageIdsToIgnoreDuringTerminationSearch,
                                                    Console.WriteLine,
                                                    line =>
                                                        {
                                                            /* no-op */
                                                        },
                                                    unzipDirPath,
                                                    environmentCertificateName,
                                                    announcementFilePath,
                                                    debugAnnouncementFilePath,
                                                    telemetryFilePath);
                        var overrideConfig = overrideDeploymentConfigJson.FromJson<DeploymentConfiguration>();

                        deploymentManager.DeployPackagesAsync(packagesToDeploy, environment, instanceName, overrideConfig).Wait();
                    }
                }
            }
        }

        /// <summary>
        /// Upload a certificate to the arcology from a file along with additional information about it as well as encrypting information.
        /// </summary>
        /// <param name="certificateWriterJson">Certificate writer configuration JSON.</param>
        /// <param name="name">Name of the certificate to load.</param>
        /// <param name="pfxFilePath">File path to the certificate to load (in PFX file format).</param>
        /// <param name="clearTextPassword">Clear text password of the certificate to load.</param>
        /// <param name="certificateSigningRequestPemEncodedFilePath">File path to Certificate Signing Request (PEM encoded).</param>
        /// <param name="encryptingCertificateThumbprint">Thumbprint of the encrypting certificate.</param>
        /// <param name="encryptingCertificateIsValid">Value indicating whether or not the encrypting certificate is valid.</param>
        /// <param name="encryptingCertificateStoreName"><see cref="StoreName"/> to find the encrypting certificate.</param>
        /// <param name="encryptingCertificateStoreLocation"><see cref="StoreLocation"/> to find the encrypting certificate.</param>
        /// <param name="startDebugger">A value indicating whether or not to launch the debugger.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "certificateWriterJson", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "encryptingCertificateStoreLocation", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "encryptingCertificateStoreName", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "encryptingCertificateIsValid", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "encryptingCertificateThumbprint", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "cleanPassword", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "pfxFilePath", Justification = "Name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "3", Justification = "Is validated with Must.")]
        [Verb(Aliases = "deploy", Description = "Deploys a new instance with specified packages.")]
        public static void UploadCertificate(
                [Aliases("")] [Description("Certificate writer configuration JSON.")] string certificateWriterJson,
                [Aliases("")] [Description("Name of the certificate to load.")] string name,
                [Aliases("")] [Description("File path to the certificate to load (in PFX file format).")] string pfxFilePath,
                [Aliases("")] [Description("Clear text password of the certificate to load.")] string clearTextPassword,
                [Aliases("")] [DefaultValue(null)] [Description("File path to Certificate Signing Request (PEM encoded).")] string certificateSigningRequestPemEncodedFilePath,
                [Aliases("")] [Description("Thumbprint of the encrypting certificate.")] string encryptingCertificateThumbprint,
                [Aliases("")] [Description("Value indicating whether or not the encrypting certificate is valid.")] bool encryptingCertificateIsValid,
                [Aliases("")] [DefaultValue(null)] [Description("Store name to find the encrypting certificate.")] string encryptingCertificateStoreName,
                [Aliases("")] [DefaultValue(null)] [Description("Store location to find the encrypting certificate.")] string encryptingCertificateStoreLocation,
                [Aliases("")] [Description("Start the debugger.")] [DefaultValue(false)] bool startDebugger)
        {
            if (startDebugger)
            {
                Debugger.Launch();
            }

            WriteAsciiArt();

            new { name, pfxFilePath, clearTextPassword, encryptingCertificateThumbprint }.Must().NotBeNull().And().NotBeWhiteSpace().OrThrowFirstFailure();

            if (!File.Exists(pfxFilePath))
            {
                throw new FileNotFoundException("Could not find specified PFX file path: " + pfxFilePath);
            }

            if (!string.IsNullOrWhiteSpace(certificateSigningRequestPemEncodedFilePath) && !File.Exists(certificateSigningRequestPemEncodedFilePath))
            {
                throw new FileNotFoundException("Could not find specified Certificate Signing Request (PEM Encoded) file path: " + certificateSigningRequestPemEncodedFilePath);
            }

            var cleanPassword = new string('*', clearTextPassword.Length / 2) + string.Join(string.Empty, clearTextPassword.Skip(clearTextPassword.Length / 2));

            Console.WriteLine("PARAMETERS:");
            Console.WriteLine("--                               name: " + name);
            Console.WriteLine("--                        pfxFilePath: " + pfxFilePath);
            Console.WriteLine("--                      cleanPassword: " + cleanPassword);
            Console.WriteLine("--    encryptingCertificateThumbprint: " + encryptingCertificateThumbprint);
            Console.WriteLine("--       encryptingCertificateIsValid: " + encryptingCertificateIsValid);
            Console.WriteLine("--     encryptingCertificateStoreName: " + encryptingCertificateStoreName);
            Console.WriteLine("-- encryptingCertificateStoreLocation: " + encryptingCertificateStoreLocation);
            Console.WriteLine("--              certificateWriterJson: " + certificateWriterJson);
            Console.WriteLine(string.Empty);

            Config.SetupSerialization();

            var certificateConfiguration = certificateWriterJson.FromJson<CertificateManagementConfigurationBase>();
            var writer = CertificateManagementFactory.CreateWriter(certificateConfiguration);

            var encryptingCertificateStoreNameEnum = encryptingCertificateStoreName == null
                                                         ? CertificateLocator.DefaultCertificateStoreName
                                                         : (StoreName)Enum.Parse(typeof(StoreName), encryptingCertificateStoreName);
            var encryptingCertificateStoreLocationEnum = encryptingCertificateStoreLocation == null
                                                             ? CertificateLocator.DefaultCertificateStoreLocation
                                                             : (StoreLocation)Enum.Parse(typeof(StoreLocation), encryptingCertificateStoreLocation);

            var encryptingCertificateLocator = new CertificateLocator(
                                                   encryptingCertificateThumbprint,
                                                   encryptingCertificateIsValid,
                                                   encryptingCertificateStoreNameEnum,
                                                   encryptingCertificateStoreLocationEnum);

            var bytes = File.ReadAllBytes(pfxFilePath);
            var certificateSigningRequestPemEncoded = certificateSigningRequestPemEncodedFilePath == null
                                                          ? null
                                                          : File.ReadAllText(certificateSigningRequestPemEncodedFilePath);

            var certToLoad = CertificateManagementFactory.BuildCertificateDescriptionWithClearPfxPayload(name, bytes, clearTextPassword, certificateSigningRequestPemEncoded);

            var cert = certToLoad.ToEncryptedVersion(encryptingCertificateLocator);

            using (var async = AsyncHelper.Wait)
            {
                async.Run(writer.PersistCertificateAsync(cert));
            }
        }

        private static void NugetAnnouncementAction(string output, string nugetAnnouncementFilePath)
        {
            if (!string.IsNullOrWhiteSpace(nugetAnnouncementFilePath))
            {
                lock (NugetAnnouncementFileLock)
                {
                    File.AppendAllText(nugetAnnouncementFilePath, output);
                }
            }
        }

        /// <summary>
        /// Help method to call from CLAP.
        /// </summary>
        /// <param name="helpText">Generated help text to display.</param>
        [Empty]
        [Help(Aliases = "h,?,-h,-help")]
        [Verb(IsDefault = true)]
        public static void Help(string helpText)
        {
            new { helpText }.Must().NotBeNull().OrThrowFirstFailure();

            Console.WriteLine("   Usage");
            Console.Write("   -----");

            // strip out the usage info about help, it's confusing
            helpText = helpText.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Skip(3).ToNewLineDelimited();
            Console.WriteLine(helpText);
            Console.WriteLine();
        }

        /// <summary>
        /// Error method to call from CLAP.
        /// </summary>
        /// <param name="context">Context provided with details.</param>
        [Error]
        public static void Error(ExceptionContext context)
        {
            new { context }.Must().NotBeNull().OrThrowFirstFailure();

            // change color to red
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            // parser exception or
            if (context.Exception is CommandLineParserException)
            {
                Console.WriteLine("I don't understand.  Run the exe with the 'help' command for usage.");
                Console.WriteLine("   " + context.Exception.Message);
            }
            else
            {
                Console.WriteLine("Something broke while performing duties.");
                Console.WriteLine("   " + context.Exception.Message);
                Console.WriteLine(string.Empty);
                Console.WriteLine("   " + context.Exception);
            }

            // restore color
            Console.WriteLine();
            Console.ForegroundColor = originalColor;
        }

        private static TimeSpan GetTimeSpanFromDayHourMinuteColonDelimited(string textToParse)
        {
            var argException = new ArgumentException("Value: " + (textToParse ?? string.Empty) + " isn't a valid time, please use format dd:hh:mm.", textToParse);
            if (string.IsNullOrEmpty(textToParse))
            {
                throw argException;
            }

            var split = textToParse.Split(':');
            if (split.Length != 3)
            {
                throw argException;
            }

            var daysRaw = split[0];
            int days;
            if (!int.TryParse(daysRaw, out days))
            {
                throw argException;
            }

            var hoursRaw = split[1];
            int hours;
            if (!int.TryParse(hoursRaw, out hours))
            {
                throw argException;
            }

            var minutesRaw = split[2];
            int minutes;
            if (!int.TryParse(minutesRaw, out minutes))
            {
                throw argException;
            }

            return new TimeSpan(days, hours, minutes, 0);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "xxxx", Justification = "Not real text.")]
        private static void WriteAsciiArt()
        {
            Console.WriteLine(@"<:::::::::::::::::::::::::::::::::::::::::}]xxxx()o             ");
            Console.WriteLine(@"  _   _          ____   _____  _____             _              ");
            Console.WriteLine(@" | \ | |   /\   / __ \ / ____||  __ \           | |             ");
            Console.WriteLine(@" |  \| |  /  \ | |  | | (___  | |  | | ___ _ __ | | ___  _   _  ");
            Console.WriteLine(@" | . ` | / /\ \| |  | |\___ \ | |  | |/ _ \ '_ \| |/ _ \| | | | ");
            Console.WriteLine(@" | |\  |/ ____ \ |__| |____) || |__| |  __/ |_) | | (_) | |_| | ");
            Console.WriteLine(@" |_| \_/_/    \_\____/|_____(_)_____/ \___| .__/|_|\___/ \__, | ");
            Console.WriteLine(@"                                          | |             __/ | ");
            Console.WriteLine(@"                                          |_|            |___/  ");
            Console.WriteLine(@"             o()xxxx[{:::::::::::::::::::::::::::::::::::::::::>");
            Console.WriteLine(string.Empty);
            Console.WriteLine(string.Empty);
        }
    }
}