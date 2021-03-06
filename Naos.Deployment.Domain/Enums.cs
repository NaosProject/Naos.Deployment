﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Enums.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Enumeration of the various types of environments.
    /// </summary>
    public enum EnvironmentType
    {
        /// <summary>
        /// Amazon Web Services.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Aws", Justification = "Spelling/name is correct.")]
        Aws,

        /// <summary>
        /// Microsoft Azure
        /// </summary>
        Azure,

        /// <summary>
        /// Microsoft Windows Server Hyper-V.
        /// </summary>
        Hypervisor,

        /// <summary>
        /// Manual deployment to server, builds a deployment package.
        /// </summary>
        Manual,

        /// <summary>
        /// VM Ware.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Vm", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Vm", Justification = "Spelling/name is correct.")]
        VmWare,
    }

    /// <summary>
    /// Enumeration of the types of deployed status of a package.
    /// </summary>
    public enum PackageDeploymentStatus
    {
        /// <summary>
        /// Nothing specified.
        /// </summary>
        Unknown,

        /// <summary>
        /// Package is known to not yet be deployed.
        /// </summary>
        NotYetDeployed,

        /// <summary>
        /// Package was deployed successfully.
        /// </summary>
        DeployedSuccessfully,
    }

    /// <summary>
    /// Enumeration of the types of startup modes for IIS Application Pools.
    /// </summary>
    public enum ApplicationPoolStartMode
    {
        /// <summary>
        /// Nothing specified.
        /// </summary>
        None,

        /// <summary>
        /// IIS will manage as necessary with traffic.
        /// </summary>
        OnDemand,

        /// <summary>
        /// IIS will keep it running all the time.
        /// </summary>
        AlwaysRunning,
    }

    /// <summary>
    /// Enumeration of the type of access an instance needs.
    /// </summary>
    public enum InstanceAccessibility
    {
        /// <summary>
        /// Indicates that it's irrelevant to this deployment.
        /// </summary>
        DoesNotMatter,

        /// <summary>
        /// Indicates accessible only inside the private network.
        /// </summary>
        Private,

        /// <summary>
        /// Indicates accessible to the public internet.
        /// </summary>
        Public,

        /// <summary>
        /// Indicates accessible to public internet but for tunnel purposes.
        /// </summary>
        Tunnel,
    }

    /// <summary>
    /// Enumeration of the different SKU's of windows that are available.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sku", Justification = "Spelling/name is correct.")]
    public enum WindowsSku
    {
        /// <summary>
        /// Indicates that it's irrelevant to this deployment.
        /// </summary>
        DoesNotMatter,

        /// <summary>
        /// Core SKU (without UI or SQL).
        /// </summary>
        Core,

        /// <summary>
        /// Base SKU (without SQL).
        /// </summary>
        Base,

        /// <summary>
        /// SQL Web SKU.
        /// </summary>
        SqlWeb,

        /// <summary>
        /// SQL Standard SKU.
        /// </summary>
        SqlStandard,

        /// <summary>
        /// SQL Enterprise SKU.
        /// </summary>
        SqlEnterprise,

        /// <summary>
        /// Unused because a specific image was supplied.
        /// </summary>
        SpecificImageSupplied,
    }

    /// <summary>
    /// Enumeration of the different distributions of linux available.
    /// </summary>
    public enum LinuxDistribution
    {
        /// <summary>
        /// Indicates that it's irrelevant to this deployment.
        /// </summary>
        DoesNotMatter,

        /// <summary>
        /// Unused because a specific image was supplied.
        /// </summary>
        SpecificImageSupplied,
    }

    /// <summary>
    /// Enumeration of the types of volumes that can be created.
    /// </summary>
    public enum VolumeType
    {
        /// <summary>
        /// Indicates that it's irrelevant to this deployment.
        /// </summary>
        DoesNotMatter,

        /// <summary>
        /// Low performance volume type.
        /// </summary>
        LowPerformance,

        /// <summary>
        /// Standard volume type.
        /// </summary>
        Standard,

        /// <summary>
        /// High performance volume type.
        /// </summary>
        HighPerformance,

        /// <summary>
        /// Instance level volume, whatever is provided from computing platform.
        /// </summary>
        Instance,
    }

    /// <summary>
    /// States an instance can be in.
    /// </summary>
    public enum InstanceState
    {
        /// <summary>
        /// Unknown state.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Pending state.
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Running state.
        /// </summary>
        Running = 2,

        /// <summary>
        /// Shutting down state.
        /// </summary>
        ShuttingDown = 3,

        /// <summary>
        /// Terminated state.
        /// </summary>
        Terminated = 4,

        /// <summary>
        /// Stopping state.
        /// </summary>
        Stopping = 5,

        /// <summary>
        /// Stopped state.
        /// </summary>
        Stopped = 6,
    }

    /// <summary>
    /// State a check can be in.
    /// </summary>
    public enum CheckState
    {
        /// <summary>
        /// Unknown state.
        /// </summary>
        Unknown,

        /// <summary>
        /// Initializing state.
        /// </summary>
        Initializing,

        /// <summary>
        /// Insufficient data state.
        /// </summary>
        InsufficientData,

        /// <summary>
        /// Failed state.
        /// </summary>
        Failed,

        /// <summary>
        /// Passed state.
        /// </summary>
        Passed,
    }

    /// <summary>
    /// Strategy on how to match tags when searching.
    /// </summary>
    public enum TagMatchStrategy
    {
        /// <summary>
        /// Invalid default state.
        /// </summary>
        Invalid,

        /// <summary>
        /// Must match all tags.
        /// </summary>
        All,

        /// <summary>
        /// Must match only one tag.
        /// </summary>
        Any,
    }

    /// <summary>
    /// Slot of execution for setup steps.
    /// </summary>
    public enum SetupStepExecutionSlot
    {
        /// <summary>
        /// Invalid default.
        /// </summary>
        Invalid,

        /// <summary>
        /// Before reboot.
        /// </summary>
        PreReboot,

        /// <summary>
        /// First after reboot.
        /// </summary>
        FirstAfterReboot,

        /// <summary>
        /// Last after reboot.
        /// </summary>
        LastAfterReboot,
    }

    /// <summary>
    /// Strategy of what to do if there is an existing deployment.
    /// </summary>
    public enum ExistingDeploymentStrategy
    {
        /// <summary>
        /// Invalid default.
        /// </summary>
        Invalid,

        /// <summary>
        /// Replace existing instance.
        /// </summary>
        Replace,

        /// <summary>
        /// Deploy same instance while leaving the existing.
        /// </summary>
        DeploySideBySide,

        /// <summary>
        /// Deploy an additional instance.
        /// </summary>
        DeployAdditional,

        /// <summary>
        /// Can not replace or duplicate existing (usually something stateful like persistence).
        /// </summary>
        NotPossibleToReplaceOrDuplicate,
    }
}
