// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsoleAbstraction.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using CLAP;

    using Naos.AWS.Domain;

    using Naos.Deployment.Domain;

    using OBeautifulCode.Validation.Recipes;

    /// <summary>
    /// Deployment logic to be invoked from the console harness.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Deployer", Justification = "Spelling/name is correct.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors", Justification = "Used by CLAP.")]
    public class ConsoleAbstraction : ConsoleAbstractionBase
    {
        /// <summary>
        /// Gets new credentials on the computing platform provider, will be prepped such that output can be saved to a variable and passed back in for CredentialsJson parameter.
        /// </summary>
        /// <param name="location">Computing platform provider location to make the call against.</param>
        /// <param name="tokenLifespan">Life span of the credentials (in format dd:hh:mm).</param>
        /// <param name="username">Username of the credentials.</param>
        /// <param name="password">Password of the credentials.</param>
        /// <param name="virtualMfaDeviceId">Virtual MFA device id of the credentials.</param>
        /// <param name="mfaValue">Token from the MFA device to use when authenticating.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Optional environment name that will set the <see cref="Its.Configuration" /> precedence instead of the default which is reading the App.Config value.</param>
        /// <param name="escapeQuotes">Optional value indicating whether or not to escape quotes; DEFAULT is true.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "mfa", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mfa", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "username", Justification = "Not sure why it's complaining...")]
        [Verb(Aliases = "credentials", Description = "Gets new credentials on the computing platform provider, will be prepped such that output can be saved to a variable and passed back in for CredentialsJson parameter.")]
        public static void GetNewCredentialJson(
            [Aliases("")] [Required] [Description("Computing platform provider location to make the call against.")] string location,
            [Aliases("")] [Required] [Description("Life span of the credentials (in format dd:hh:mm).")] string tokenLifespan,
            [Aliases("")] [Required] [Description("Username of the credentials.")] string username,
            [Aliases("")] [Required] [Description("Password of the credentials.")] string password,
            [Aliases("")] [Required] [Description("Virtual MFA device id of the credentials.")] string virtualMfaDeviceId,
            [Aliases("")] [Required] [Description("Token from the MFA device to use when authenticating.")] string mfaValue,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment,
            [Aliases("")] [Description("Optional value indicating whether or not to escape quotes; DEFAULT is true.")] [DefaultValue(true)] bool escapeQuotes)
        {
            CommonSetup(debug, environment);

            var tokenLifespanTimeSpan = ConsoleAbstractionBase.ParseTimeSpanFromDayHourMinuteColonDelimited(tokenLifespan);
            NaosDeploymentBootstrapper.GetNewCredentialsJson(location, tokenLifespanTimeSpan, username, password, virtualMfaDeviceId, mfaValue, escapeQuotes, Console.Write);
        }

        /// <summary>
        /// Gets the password of an instance from the provided tracker.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="instanceName">Name of the computer (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name where instance to get password is located.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "password", Description = "Gets the password of an instance from the provided tracker.")]
        public static void GetPassword(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Required] [Description("Name of the computer (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').")] string instanceName,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.GetPassword(credentialsJson, infrastructureTrackerJson, instanceName, environment, Console.WriteLine);
        }

        /// <summary>
        /// Gets the status of the instance found by name in provided tracker.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="instanceName">Name of the computer (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name being deployed to.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "status", Description = "Gets the status of the instance found by name in provided tracker.")]
        public static void GetInstanceStatus(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Required] [Description("Name of the instance to start (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').")] string instanceName,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.GetInstanceStatus(credentialsJson, infrastructureTrackerJson, instanceName, environment, Console.WriteLine);
        }

        /// <summary>
        /// Gets the instances that are active (not terminated) from the underlying computing provider.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name being deployed to.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "query", Description = "Gets the instances that are active (not terminated) from the underlying computing provider.")]
        public static void GetActiveInstancesFromProvider(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.GetActiveInstancesFromProvider(credentialsJson, infrastructureTrackerJson, environment, Console.WriteLine);
        }

        /// <summary>
        /// Gets the instance names (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a') in provided tracker.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name being deployed to.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "list", Description = "Gets the instance names (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a') in provided tracker.")]
        public static void GetInstanceNames(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.GetInstanceNames(credentialsJson, infrastructureTrackerJson, environment, Console.WriteLine);
        }

        /// <summary>
        /// Gets the instances only in either tracking or computer platform.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name to check.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "diff", Description = "Gets the instances only in either tracking or computer platform.")]
        public static void GetInstancesInTrackingAndNotProviderOrReverse(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.GetInstancesInTrackingAndNotProviderOrReverse(credentialsJson, infrastructureTrackerJson, environment, Console.WriteLine);
        }

        /// <summary>
        /// Removes an instance from tracking that is not in the computing platform.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="instanceName">Name of instance to remove.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name where instance should be removed.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "retire", Description = "Gets the instances only in either tracking or computer platform.")]
        public static void RemoveTrackedInstance(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("name")] [Required] [Description("Name of instance to remove.")] string instanceName,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.RemoveTrackedInstance(credentialsJson, infrastructureTrackerJson, instanceName, environment, Console.WriteLine);
        }

        /// <summary>
        /// Removes an instance from tracking that is not in the computing platform.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="privateIpAddressOfInstanceToRemove">IP Address of instance to remove (cannot be used with <paramref name="instanceNameOfInstanceToRemove" />).</param>
        /// <param name="instanceNameOfInstanceToRemove">Name of instance to remove (cannot be used with <paramref name="privateIpAddressOfInstanceToRemove" />).</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name with un-deployed instance.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "purge", Description = "Removes an instance from tracking that is not in the computing platform.")]
        public static void RemoveTrackedInstanceNotInComputingPlatform(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("ip")] [Description("IP Address of instance to remove.")] string privateIpAddressOfInstanceToRemove,
            [Aliases("name")] [Description("Name of instance to remove.")] string instanceNameOfInstanceToRemove,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.RemoveTrackedInstanceNotInComputingPlatform(credentialsJson, infrastructureTrackerJson, privateIpAddressOfInstanceToRemove, instanceNameOfInstanceToRemove, environment, Console.WriteLine);
        }

        /// <summary>
        /// Removes an instance from the computing platform that is not in tracking.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="systemIdOfInstanceToRemove">ID of instance to remove (ID from the computing platform).</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name with un-tracked instance.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "kill", Description = "Removes an instance from the computing platform that is not in tracking.")]
        public static void RemoveInstanceInComputingPlatformNotTracked(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("id")] [Required] [Description("ID of instance to remove (ID from the computing platform).")] string systemIdOfInstanceToRemove,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            new { systemIdOfInstanceToRemove }.Must().NotBeNullNorWhiteSpace();

            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.RemoveInstanceInComputingPlatformNotTracked(credentialsJson, infrastructureTrackerJson, systemIdOfInstanceToRemove, environment, Console.WriteLine);
        }

        /// <summary>
        /// Gets the details of the instance found by name in provided tracker.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="instanceName">Name of the computer (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name where instance is located.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "details", Description = "Gets the details of the instance found by name in provided tracker.")]
        public static void GetInstanceDetails(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Required] [Description("Name of the instance to start (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').")] string instanceName,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.GetInstanceDetails(credentialsJson, infrastructureTrackerJson, instanceName, environment, Console.WriteLine);
        }

        /// <summary>
        /// Starts a remote session instance found by name in provided tracker.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="instanceName">Name of the computer (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <param name="shouldConnectInFullScreen">A value indicating whether or not to connect in full screen mode.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name where instance is located.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "This is fine.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "connect", Description = "Starts a remote session instance found by name in provided tracker.")]
        public static void ConnectToInstance(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Required] [Description("Name of the instance to start (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').")] string instanceName,
            [Aliases("fullscreen")] [Description("Connect in fullscreen mode.")] [DefaultValue(true)] bool shouldConnectInFullScreen,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.ConnectToInstance(credentialsJson, infrastructureTrackerJson, instanceName, environment, shouldConnectInFullScreen);
        }

        /// <summary>
        /// Starts the instance found by name in provided tracker.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="instanceName">Name of the computer (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name where instance is located.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "start", Description = "Starts the instance found by name in provided tracker.")]
        public static void StartInstance(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Required] [Description("Name of the instance to start (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').")] string instanceName,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.StartInstance(credentialsJson, infrastructureTrackerJson, instanceName, environment);
        }

        /// <summary>
        /// Stops the instance found by name in provided tracker.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="instanceName">Name of the computer (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <param name="force">Force the shutdown.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name where instance is located.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "stop", Description = "Stops the instance found by name in provided tracker.")]
        public static void StopInstance(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Required] [Description("Name of the instance to start (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').")] string instanceName,
            [Aliases("")] [Description("Force the shutdown.")] [DefaultValue(false)] bool force,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.StopInstance(credentialsJson, infrastructureTrackerJson, instanceName, force, environment);
        }

        /// <summary>
        /// Stops then starts the instance found by name in provided tracker.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="instanceName">Name of the computer (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').</param>
        /// <param name="force">Force the shutdown.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name where instance is located.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "bounce", Description = "Stops then starts the instance found by name in provided tracker.")]
        public static void StopThenStartInstance(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Required] [Description("Name of the instance to start (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a').")] string instanceName,
            [Aliases("")] [Description("Force the shutdown.")] [DefaultValue(false)] bool force,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.StopThenStartInstance(credentialsJson, infrastructureTrackerJson, instanceName, force, environment);
        }

        /// <summary>
        /// Deploys a new instance with specified packages.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="nugetPackageRepositoryConfigurationsJson">NuGet Repository/Gallery configurations.</param>
        /// <param name="certificateRetrieverJson">Certificate retriever configuration JSON.</param>
        /// <param name="infrastructureTrackerJson">Configuration for tracking system of computing infrastructure.</param>
        /// <param name="overrideDeploymentConfigJson">Optional deployment configuration to use as an override in JSON.</param>
        /// <param name="environmentCertificateName">Optional certificate name for an environment certificate saved in certificate manager being configured.</param>
        /// <param name="announcementFilePath">Optional announcement file path to write a JSON file of announcements (will overwrite if existing).</param>
        /// <param name="debugAnnouncementFilePath">Optional announcement file path to write a JSON file of debug announcements (will overwrite if existing)</param>
        /// <param name="telemetryFilePath">Optional telemetry file path to write a JSON file of certain step timings (will overwrite if existing).</param>
        /// <param name="nugetAnnouncementFilePath">Optional nuget file path to write a JSON file of output from nuget (will overwrite if existing).</param>
        /// <param name="instanceName">Optional name of the instance (one will be generated from the package list if not provided).</param>
        /// <param name="workingPath">Optional working directory for packages (default will be Temp Dir but might result in PathTooLongException).</param>
        /// <param name="packagesToDeployJson">Optional packages descriptions (with overrides) to configure the instance with.</param>
        /// <param name="deploymentAdjustmentApplicatorJson">Optional deployment adjustment strategies to use.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Environment name being deployed to.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "nuget", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "telemetryFilePath", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "nugetAnnouncementFilePath", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "debugAnnouncementFilePath", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "announcementFilePath", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "environmentCertificateName", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "packagesToDeployJson", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "instanceName", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "overrideDeploymentConfigJson", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "infrastructureTrackerJson", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "certificateRetrieverJson", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "nugetPackageRepositoryConfigurationJson", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "deploymentAdjustmentApplicatorJson", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "credentialsJson", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "workingPath", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "nuget", Justification = "Not sure why it's complaining...")]
        [Verb(Aliases = "deploy", Description = "Deploys a new instance with specified packages.")]
        public static void Deploy(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Aliases("")] [Required] [Description("NuGet Repository/Gallery configuration.")] string nugetPackageRepositoryConfigurationsJson,
            [Aliases("")] [Required] [Description("Certificate retriever configuration JSON.")] string certificateRetrieverJson,
            [Aliases("")] [Required] [Description("Configuration for tracking system of computing infrastructure.")] string infrastructureTrackerJson,
            [Aliases("")] [Description("Optional deployment configuration to use as an override in JSON.")] [DefaultValue(null)] string overrideDeploymentConfigJson,
            [Aliases("")] [Description("Optional certificate name for an environment certificate saved in certificate manager being configured.")] [DefaultValue(null)] string environmentCertificateName,
            [Aliases("")] [Description("Optional announcement file path to write a JSON file of announcements (will overwrite if existing).")] [DefaultValue(null)] string announcementFilePath,
            [Aliases("")] [Description("Optional announcement file path to write a JSON file of debug announcements (will overwrite if existing).")] [DefaultValue(null)] string debugAnnouncementFilePath,
            [Aliases("")] [Description("Optional telemetry file path to write a JSON file of certain step timings (will overwrite if existing).")] [DefaultValue(null)] string telemetryFilePath,
            [Aliases("")] [Description("Optional nuget file path to write a JSON file of output from nuget (will overwrite if existing).")] [DefaultValue(null)] string nugetAnnouncementFilePath,
            [Aliases("")] [Description("Optional name of the instance (one will be generated from the package list if not provided).")] [DefaultValue(null)] string instanceName,
            [Aliases("")] [Description("Optional working directory for packages (default will be Temp Dir but might result in PathTooLongException).")] [DefaultValue(null)] string workingPath,
            [Aliases("")] [Description("Optional packages descriptions (with overrides) to configure the instance with.")] [DefaultValue("[]")] string packagesToDeployJson,
            [Aliases("")] [Description("Optional deployment adjustment strategies to use.")] [DefaultValue("[]")] string deploymentAdjustmentApplicatorJson,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            PrintArguments(
                new
                {
                    workingPath,
                    credentialsJson,
                    deploymentAdjustmentApplicatorJson,
                    nugetPackageRepositoryConfigurationsJson,
                    certificateRetrieverJson,
                    infrastructureTrackerJson,
                    overrideDeploymentConfigJson,
                    environment,
                    instanceName,
                    packagesToDeployJson,
                    environmentCertificateName,
                    announcementFilePath,
                    debugAnnouncementFilePath,
                    nugetAnnouncementFilePath,
                    telemetryFilePath,
                });

            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.Deploy(credentialsJson, nugetPackageRepositoryConfigurationsJson, certificateRetrieverJson, infrastructureTrackerJson, overrideDeploymentConfigJson, environmentCertificateName, announcementFilePath, debugAnnouncementFilePath, telemetryFilePath, nugetAnnouncementFilePath, instanceName, workingPath, packagesToDeployJson, deploymentAdjustmentApplicatorJson, environment);
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
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Optional environment name that will set the <see cref="Its.Configuration" /> precedence instead of the default which is reading the App.Config value.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pem", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Like it this way.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "certificateWriterJson", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "encryptingCertificateStoreLocation", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "encryptingCertificateStoreName", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "encryptingCertificateIsValid", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "encryptingCertificateThumbprint", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "cleanPassword", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "pfxFilePath", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "3", Justification = "Is validated with Must.")]
        [Verb(Aliases = "upload", Description = "Deploys a new instance with specified packages.")]
        public static void UploadCertificate(
            [Aliases("")] [Required] [Description("Certificate writer configuration JSON.")] string certificateWriterJson,
            [Aliases("")] [Required] [Description("Name of the certificate to load.")] string name,
            [Aliases("")] [Required] [Description("File path to the certificate to load (in PFX file format).")] string pfxFilePath,
            [Aliases("")] [Required] [Description("Clear text password of the certificate to load.")] string clearTextPassword,
            [Aliases("")] [DefaultValue(null)] [Description("File path to Certificate Signing Request (PEM encoded).")] string certificateSigningRequestPemEncodedFilePath,
            [Aliases("")] [Required] [Description("Thumbprint of the encrypting certificate.")] string encryptingCertificateThumbprint,
            [Aliases("")] [Required] [Description("Value indicating whether or not the encrypting certificate is valid.")] bool encryptingCertificateIsValid,
            [Aliases("")] [DefaultValue(null)] [Description("Store name to find the encrypting certificate.")] string encryptingCertificateStoreName,
            [Aliases("")] [DefaultValue(null)] [Description("Store location to find the encrypting certificate.")] string encryptingCertificateStoreLocation,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("")] [Description("Sets the Its.Configuration precedence to use specific settings.")] [DefaultValue(null)] string environment)
        {
            CommonSetup(debug, environment);

            PrintArguments(
                new
                {
                    name,
                    pfxFilePath,
                    maskedPassword = MaskString(clearTextPassword),
                    encryptingCertificateThumbprint,
                    encryptingCertificateIsValid,
                    encryptingCertificateStoreName,
                    encryptingCertificateStoreLocation,
                    certificateWriterJson,
                });

            NaosDeploymentBootstrapper.UploadCertificate(certificateWriterJson, name, pfxFilePath, clearTextPassword, certificateSigningRequestPemEncodedFilePath, encryptingCertificateThumbprint, encryptingCertificateIsValid, encryptingCertificateStoreName, encryptingCertificateStoreLocation);
        }

        /// <summary>
        /// Creates a new environment.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="configFilePath">XML Serialized <see cref="ConfigEnvironment "/> describing assets to create.</param>
        /// <param name="outputArcologyPath">File path to create a file based arcology at.</param>
        /// <param name="computingPlatformKeyFilePath">Key file from computing provider for creating assets.</param>
        /// <param name="environmentCertificateFilePath">Certificate file to use for each machine.</param>
        /// <param name="environmentCertificatePassword">Password for environment certificate file.</param>
        /// <param name="deploymentCertificateFilePath">Certificate file to use for encrypting sensitive data.</param>
        /// <param name="deploymentCertificatePassword">Password for the deployment certificate file.</param>
        /// <param name="windowsSkuSearchPatternMapJson">Map of <see cref="WindowsSku" /> to search pattern to find appropriate instance template.</param>
        /// <param name="rootDomainHostingIdMapJson">Map of root domain to root hosting ID for computing platform.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Optional environment name that will set the <see cref="Its.Configuration" /> precedence instead of the default which is reading the App.Config value.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "All disposables are disposed, not sure why it's upset about the Creator.CreateEnvironment call.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This is fine.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sku", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "create", Description = "Creates a new environment.")]
        public static void CreateEnvironment(
                [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
                [Required] [Aliases("file")] [Description("XML Serialized ConfigEnvironment describing assets to create.")] string configFilePath,
                [Required] [Aliases("path")] [Description("File path to create a file based arcology at.")] string outputArcologyPath,
                [Required] [Aliases("")] [Description("ey file from computing provider for creating assets.")] string computingPlatformKeyFilePath,
                [Required] [Aliases("")] [Description("Certificate to encrypt the key.")] string environmentCertificateFilePath,
                [Required] [Aliases("")] [Description("Certificate file to use for each machine.")] string environmentCertificatePassword,
                [Required] [Aliases("")] [Description("Password for environment certificate file.")] string deploymentCertificateFilePath,
                [Required] [Aliases("")] [Description("Password for the deployment certificate file.")] string deploymentCertificatePassword,
                [Required] [Aliases("")] [Description("Map of WindowsSku to search pattern to find appropriate instance template.")] string windowsSkuSearchPatternMapJson,
                [Required] [Aliases("")] [Description("Map of root domain to root hosting ID for computing platform.")] string rootDomainHostingIdMapJson,
                [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
                [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            new { credentialsJson }.Must().NotBeNullNorWhiteSpace();
            new { configFilePath }.Must().NotBeNullNorWhiteSpace();
            new { outputArcologyPath }.Must().NotBeNullNorWhiteSpace();
            new { computingPlatformKeyFilePath }.Must().NotBeNullNorWhiteSpace();
            new { environmentCertificateFilePath }.Must().NotBeNullNorWhiteSpace();
            new { environmentCertificatePassword }.Must().NotBeNullNorWhiteSpace();
            new { deploymentCertificateFilePath }.Must().NotBeNullNorWhiteSpace();
            new { deploymentCertificatePassword }.Must().NotBeNullNorWhiteSpace();
            new { windowsSkuSearchPatternMapJson }.Must().NotBeNullNorWhiteSpace();
            new { rootDomainHostingIdMapJson }.Must().NotBeNullNorWhiteSpace();

            NaosDeploymentBootstrapper.CreateEnvironment(credentialsJson, configFilePath, outputArcologyPath, computingPlatformKeyFilePath, environmentCertificateFilePath, environmentCertificatePassword, deploymentCertificateFilePath, deploymentCertificatePassword, windowsSkuSearchPatternMapJson, rootDomainHostingIdMapJson, environment);
        }

        /// <summary>
        /// Destroy an existing environment.
        /// </summary>
        /// <param name="credentialsJson">Credentials for the computing platform provider to use in JSON.</param>
        /// <param name="configFilePath">XML Serialized <see cref="ConfigEnvironment "/> describing assets to destroy.</param>
        /// <param name="debug">A value indicating whether or not to launch the debugger.</param>
        /// <param name="environment">Optional environment name that will set the <see cref="Its.Configuration" /> precedence instead of the default which is reading the App.Config value.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "populatedEnvironment", Justification = "Keeping for visibility and use if necessary.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is fine.")]
        [Verb(Aliases = "destroy", Description = "Destroy an existing environment.")]
        public static void DestroyEnvironment(
            [Aliases("")] [Required] [Description("Credentials for the computing platform provider to use in JSON.")] string credentialsJson,
            [Required] [Aliases("file")] [Description("Configuration file path describing environment to destroy.")] string configFilePath,
            [Aliases("")] [Description("Launches the debugger.")] [DefaultValue(false)] bool debug,
            [Aliases("env")] [Required] [Description("Sets the Its.Configuration precedence to use specific settings.")] string environment)
        {
            CommonSetup(debug, environment);

            NaosDeploymentBootstrapper.DestroyEnvironment(credentialsJson, configFilePath, environment);
        }
    }
}