// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExecutionOrder.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;

    using Naos.Deployment.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Constants for order of execution control.
    /// </summary>
    public enum ExecutionOrder
    {
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable 1591

        Invalid = -1,

        InstanceLevel = 0,

        InstallIis = InstanceLevel + 1,

        CopyPackages = InstallIis + 1,

        ReplaceTokenInFiles = CopyPackages + 1,

        CreateDirectory = ReplaceTokenInFiles + 1,

        CreateEventLog = CreateDirectory + 1,

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Chocolatey", Justification = "Spelling/name is correct.")]
        Chocolatey = CreateEventLog + 1,

        InstallMongo = Chocolatey + 1,

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OneTime", Justification = "Spelling/name is correct.")]
        OneTimeBeforeReboot = InstallMongo + 1,

        Reboot = OneTimeBeforeReboot + 1,

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OneTime", Justification = "Spelling/name is correct.")]
        OneTimeAfterRebootFirst = Reboot + 1,

        InstallCertificate = OneTimeAfterRebootFirst + 1,

        SqlServer = InstallCertificate + 1,

        ConfigureMongo = SqlServer + 1,

        ScheduledTask = ConfigureMongo + 1,

        SelfHost = ScheduledTask + 1,

        ConfigureIis = SelfHost + 1,

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OneTime", Justification = "Spelling/name is correct.")]
        OneTimeAfterRebootLast = ConfigureIis + 1,

        Dns = OneTimeAfterRebootLast + 1,

        UpdateArcology = Dns + 1,

        PostDeployment = UpdateArcology + 1,

#pragma warning restore 1591
#pragma warning restore SA1602 // Enumeration items should be documented
#pragma warning restore SA1600 // Elements should be documented
    }

#pragma warning disable SA1649 // File name should match first type name
    /// <summary>
    /// Extensions for <see cref="ExecutionOrder" />.
    /// </summary>
    public static class ExecutionOrderExtensionMethods
    {
        /// <summary>
        /// Translate the <see cref="SetupStepExecutionSlot" /> to a <see cref="ExecutionOrder" />.
        /// </summary>
        /// <param name="slot"><see cref="SetupStepExecutionSlot" /> from a <see cref="InitializationStrategyOnetimeCall" /> to translate.</param>
        /// <returns>Correct <see cref="ExecutionOrder" /> to use.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OneTime", Justification = "Spelling/name is correct.")]
        public static ExecutionOrder GetOneTimeExecutionOrder(this SetupStepExecutionSlot slot)
        {
            switch (slot)
            {
                case SetupStepExecutionSlot.Invalid:
                    return ExecutionOrder.OneTimeAfterRebootLast;
                case SetupStepExecutionSlot.PreReboot:
                    return ExecutionOrder.OneTimeBeforeReboot;
                case SetupStepExecutionSlot.FirstAfterReboot:
                    return ExecutionOrder.OneTimeAfterRebootFirst;
                case SetupStepExecutionSlot.LastAfterReboot:
                    return ExecutionOrder.OneTimeAfterRebootLast;
                default:
                    throw new NotSupportedException(Invariant($"Unsupported {nameof(SetupStepExecutionSlot)} ({slot}) for {nameof(InitializationStrategyOnetimeCall)}."));
            }
        }
    }
#pragma warning restore SA1649 // File name should match first type name
}