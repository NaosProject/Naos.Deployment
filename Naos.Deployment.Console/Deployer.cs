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
    using System.IO;
    using System.Linq;

    using CLAP;

    using Its.Configuration;

    using Naos.AWS.Contract;
    using Naos.Deployment.CloudManagement;
    using Naos.Deployment.Contract;
    using Naos.Deployment.Core;
    using Naos.Deployment.Core.CertificateManagement;
    using Naos.Deployment.Core.CloudInfrastructureTracking;
    using Naos.Packaging.Domain;

    using Newtonsoft.Json;

    using OBeautifulCode.Libs.Collections;

    using Spritely.Recipes;

    /// <summary>
    /// Deployment logic to be invoked from the console harness.
    /// </summary>
    public class Deployer
    {
        [Verb(Aliases = "credentials", Description = "Gets new credentials on the cloud provider.")]
#pragma warning disable 1591
        public static void GetNewCredentialJson(
            [Aliases("")] [Description("Cloud provider location to make the call against.")] string location,
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
            var retObj = CloudInfrastructureManager.GetNewCredentials(
                location,
                tokenLifespanTimeSpan,
                username,
                password,
                virtualMfaDeviceId,
                mfaValue);

            var ret = Serializer.Serialize(retObj, false);
            Console.Write(ret);
        }

        [Verb(Aliases = "deploy", Description = "Deploys a new instance with specified packages.")]
#pragma warning disable 1591
        public static void Deploy(
            [Aliases("")] [Description("Credentials for the cloud provider to use in JSON.")] string cloudCredentialsJson, 
            [Aliases("")] [Description("NuGet Repository/Gallery configuration.")] string nugetPackageRepositoryConfigurationJson, 
            [Aliases("")] [Description("Message bus persistence connection string.")] [DefaultValue(null)] string messageBusPersistenceConnectionString,
            [Aliases("")] [Description("Full file path of the certificate certificate retriever managing file.")] string certificateRetrieverFilePath,
            [Aliases("")] [Description("Full folder path of the location of persistence for tracking system for cloud properties.")] string trackingSystemRootFolder, 
            [Aliases("")] [Description("Optional deployment configuration to use as an override in JSON.")] [DefaultValue(null)] string overrideDeploymentConfigJson,
            [Aliases("")] [Description("Environment to deploy to.")] string environment, 
            [Aliases("")] [Description("Optional name of the instance (one will be generated from the package list if not provided).")] [DefaultValue(null)] string instanceName,
            [Aliases("")] [Description("Optional working directory for packages (default will be Temp Dir but might result in PathTooLongException).")] [DefaultValue(null)] string workingPath, 
            [Aliases("")] [Description("Optional packages descriptions (with overrides) to configure the instance with.")] [DefaultValue("[]")] string packagesToDeployJson, 
            [Aliases("")] [Description("Start the debugger.")] [DefaultValue(false)] bool startDebugger)
#pragma warning restore 1591
        {
            if (startDebugger)
            {
                Debugger.Launch();
            }

            Console.WriteLine("PARAMETERS:");
            Console.WriteLine("--                                       workingPath: " + workingPath);
            Console.WriteLine("--                              cloudCredentialsJson: " + cloudCredentialsJson);
            Console.WriteLine("--           nugetPackageRepositoryConfigurationJson: " + nugetPackageRepositoryConfigurationJson);
            Console.WriteLine("--                      certificateRetrieverFilePath: " + certificateRetrieverFilePath);
            Console.WriteLine("--                          trackingSystemRootFolder: " + trackingSystemRootFolder);
            Console.WriteLine("--                      overrideDeploymentConfigJson: " + overrideDeploymentConfigJson);
            Console.WriteLine("--                                       environment: " + environment);
            Console.WriteLine("--                                      instanceName: " + instanceName);
            Console.WriteLine("--                              packagesToDeployJson: " + packagesToDeployJson);
            Console.WriteLine(string.Empty);

            JsonConvert.DefaultSettings = () => JsonConfiguration.DefaultSerializerSettings;
            Settings.Deserialize = Serializer.Deserialize;

            var packagesToDeploy =
                Serializer.Deserialize<ICollection<PackageDescriptionWithOverrides>>(packagesToDeployJson);

            var setupFactorySettings = Settings.Get<SetupStepFactorySettings>();
            var cloudInfrastructureManagerSettings = Settings.Get<CloudInfrastructureManagerSettings>();
            var defaultDeploymentConfiguration = Settings.Get<DefaultDeploymentConfiguration>();
            var messageBusHandlerHarnessConfiguration = Settings.Get<MessageBusHandlerHarnessConfiguration>();

            var tracker = new RootFolderEnvironmentFolderInstanceFileTracker(trackingSystemRootFolder);
            var certManager = new CertificateRetriever(certificateRetrieverFilePath);

            var credentials = Serializer.Deserialize<CredentialContainer>(cloudCredentialsJson);
            var cloudManager = new CloudInfrastructureManager(cloudInfrastructureManagerSettings, tracker).InitializeCredentials(credentials);

            var tempDir = Path.GetTempPath();
            var unzipDirPath = Path.Combine(tempDir, "Naos.Deployment.Temp");
            if (!string.IsNullOrEmpty(workingPath))
            {
                unzipDirPath = workingPath;
            }

            if (Directory.Exists(unzipDirPath))
            {
                Directory.Delete(unzipDirPath, true);
            }

            Directory.CreateDirectory(unzipDirPath);

            var repoConfig =
                Serializer.Deserialize<PackageRepositoryConfiguration>(nugetPackageRepositoryConfigurationJson);

            var packageManager = PackageRetrieverFactory.BuildPackageRetriever(repoConfig, unzipDirPath);

            var deploymentManager = new DeploymentManager(
                tracker,
                cloudManager,
                packageManager,
                certManager,
                defaultDeploymentConfiguration,
                messageBusHandlerHarnessConfiguration,
                setupFactorySettings,
                messageBusPersistenceConnectionString,
                cloudInfrastructureManagerSettings.PackageIdsToIgnoreDuringTerminationSearch,
                Console.WriteLine);

            var overrideConfig = Serializer.Deserialize<DeploymentConfiguration>(overrideDeploymentConfigJson);

            deploymentManager.DeployPackages(packagesToDeploy, environment, instanceName, overrideConfig);
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
    }
}