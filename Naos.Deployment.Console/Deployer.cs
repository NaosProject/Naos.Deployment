// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Deployer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System;
    using System.IO;
    using System.Linq;

    using CLAP;

    using Naos.AWS.Contract;
    using Naos.Deployment.Contract;
    using Naos.Deployment.Core;

    using Newtonsoft.Json;

    using OBeautifulCode.Libs.Collections;

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
                System.Diagnostics.Debugger.Launch();
            }

            var tokenLifespanTimeSpan = GetTimeSpanFromDayHourMinuteColonDelimited(tokenLifespan);
            var retObj = CloudInfrastructureManager.GetNewCredentials(
                location,
                tokenLifespanTimeSpan,
                username,
                password,
                virtualMfaDeviceId,
                mfaValue);

            var ret = JsonConvert.SerializeObject(retObj);
            Console.Write(ret);
        }

        [Verb(Aliases = "deploy", Description = "Deploys a new instance with specified packages.")]
#pragma warning disable 1591
        public static void Deploy(
            [Aliases("")] [Description("Credentials for the cloud provider to use in JSON.")] string cloudCredentialsJson,
            [Aliases("")] [Description("NuGet Repository/Gallery configuration.")] string nugetPackageRepositoryConfigurationJson,
            [Aliases("")] [Description("Full file path to the tracking file of cloud properties.")] string trackingFilePath,
            [Aliases("")] [Description("Default deployment configuration to use where items are not specified in JSON.")] string defaultDeploymentConfigJson,
            [Aliases("")] [Description("Optional deployment configuration to use as an override in JSON.")] [DefaultValue(null)] string overrideDeploymentConfigJson,
            [Aliases("")] [Description("Environment to deploy to.")] string environment,
            [Aliases("")] [Description("Optional name of the instance (one will be generated from the package list if not provided).")] [DefaultValue(null)] string instanceName,
            [Aliases("")] [Description("Optional packages to configure the instance with.")] [DefaultValue("[]")] string packagesToDeployJson,
            [Aliases("")] [Description("Start the debugger.")] [DefaultValue(false)] bool startDebugger)
#pragma warning restore 1591
        {
            if (startDebugger)
            {
                System.Diagnostics.Debugger.Launch();
            }

            Console.WriteLine("PARAMETERS:");
            Console.WriteLine("--                    cloudCredentialsJson: " + cloudCredentialsJson);
            Console.WriteLine("-- nugetPackageRepositoryConfigurationJson: " + nugetPackageRepositoryConfigurationJson);
            Console.WriteLine("--                        trackingFilePath: " + trackingFilePath);
            Console.WriteLine("--             defaultDeploymentConfigJson: " + defaultDeploymentConfigJson);
            Console.WriteLine("--            overrideDeploymentConfigJson: " + overrideDeploymentConfigJson);
            Console.WriteLine("--                             environment: " + environment);
            Console.WriteLine("--                            instanceName: " + instanceName);
            Console.WriteLine("--                    packagesToDeployJson: " + packagesToDeployJson);
            Console.WriteLine(string.Empty);

            var packagesToDeploy = JsonConvert.DeserializeObject<PackageDescription[]>(packagesToDeployJson);

            var tracker = new ComputingInfrastructureTracker(trackingFilePath);
            var credentials = JsonConvert.DeserializeObject<CredentialContainer>(cloudCredentialsJson);
            var cloudManager = new CloudInfrastructureManager(tracker).InitializeCredentials(credentials);

            var tempDir = Path.GetTempPath();
            var unzipDirPath = Path.Combine(tempDir, "Naos.Deployment.WorkingDirectory");
            var repoConfig =
                JsonConvert.DeserializeObject<PackageRepositoryConfiguration>(nugetPackageRepositoryConfigurationJson);

            var packageManager = new PackageManager(repoConfig, unzipDirPath);
            var defaultDeploymentConfig =
                DeploymentConfigurationSerializer.DeserializeDeploymentConfiguration(defaultDeploymentConfigJson);

            var deploymentManager = new DeploymentManager(
                tracker,
                cloudManager,
                packageManager,
                tracker,
                defaultDeploymentConfig,
                Console.WriteLine);

            var overrideConfig =
                DeploymentConfigurationSerializer.DeserializeDeploymentConfiguration(overrideDeploymentConfigJson);

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