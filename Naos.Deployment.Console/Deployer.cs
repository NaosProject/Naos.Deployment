// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Deployer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using static System.FormattableString;
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

    /// <summary>
    /// Deployment logic to be invoked from the console harness.
    /// </summary>
    public class Deployer
    {
        private static readonly object NugetAnnouncementFileLock = new object();

        [Verb(Aliases = "credentials", Description = "Gets new credentials on the computing platform provider, will be prepped such that output can be saved to a variable and passed back in for CredentialsJson parameter.")]
#pragma warning disable 1591
        public static void GetNewCredentialJson(
            [Aliases("")] [Description("Computing platform provider location to make the call against.")] string location,
            [Aliases("")] [Description("Life span of the credentials (in format dd:hh:mm).")] string tokenLifespan,
            [Aliases("")] [Description("Username of the credentials.")] string username,
            [Aliases("")] [Description("Password of the credentials.")] string password,
            [Aliases("")] [Description("Virtual MFA device id of the credentials.")] string virtualMfaDeviceId,
            [Aliases("")] [Description("Token from the MFA device to use when authenticating.")] string mfaValue,
            [Aliases("")] [Description("Start the debugger.")] [DefaultValue(false)] bool startDebugger)
#pragma warning restore 1591
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
            var noNewLines = rawRet.Replace(Environment.NewLine, string.Empty);
            var escapedQuotes = noNewLines.Replace("\"", "\\\"");

            var ret = escapedQuotes;
            Console.Write(ret);
        }

        [Verb(Aliases = "deploy", Description = "Deploys a new instance with specified packages.")]
#pragma warning disable 1591
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
#pragma warning restore 1591
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
            var infrastructureTracker = InfrastructureTrackerFactory.Create(infrastructureTrackerConfiguration);

            var credentials = credentialsJson.FromJson<CredentialContainer>();
            var computingManager = new ComputingInfrastructureManagerForAws(computingInfrastructureManagerSettings, infrastructureTracker).InitializeCredentials(credentials);

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

        [Verb(Aliases = "deploy", Description = "Deploys a new instance with specified packages.")]
#pragma warning disable 1591
        public static void UploadCertficate(
                [Aliases("")] [Description("Certificate writer configuration JSON.")] string certificateWriterJson,
                [Aliases("")] [Description("Name of the certificate to load.")] string name,
                [Aliases("")] [Description("File path to the certificate to load (in PFX file format).")] string pfxFilePath,
                [Aliases("")] [Description("Clear text password of the certificate to load.")] string clearTextPassword,
                [Aliases("")] [DefaultValue(null)] [Description("File path to Certificate Signing Request (PEM encoded).")] string certificateSigningRequestPemEncodedFilePath,
                [Aliases("")] [Description("Thumbprint of the encrypting certificate.")] string encryptingCertificateThumbprint,
                [Aliases("")] [Description("Value indicating whether or not the encrypting certificate is valid.")] bool encryptingCertificateIsValid,
                [Aliases("")] [DefaultValue(null)] [Description("Store name to find the encrypting certificate is valid.")] string encryptingCertificateStoreName,
                [Aliases("")] [DefaultValue(null)] [Description("Store location to find the encrypting certificate is valid.")] string encryptingCertificateStoreLocation,
                [Aliases("")] [Description("Start the debugger.")] [DefaultValue(false)] bool startDebugger)
#pragma warning restore 1591
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
                async.Run(writer.PersistCertficateAsync(cert));
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

        [Empty]
        [Help(Aliases = "h,?,-h,-help")]
        [Verb(IsDefault = true)]
#pragma warning disable 1591
        public static void Help(string help)
#pragma warning restore 1591
        {
            Console.WriteLine("   Usage");
            Console.Write("   -----");

            // strip out the usage info about help, it's confusing
            help = help.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Skip(3).ToNewLineDelimited();
            Console.WriteLine(help);
            Console.WriteLine();
        }

        [Error]
#pragma warning disable 1591
        public static void Error(ExceptionContext context)
#pragma warning restore 1591
        {
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