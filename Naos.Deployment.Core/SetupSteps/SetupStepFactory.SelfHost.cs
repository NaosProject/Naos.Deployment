// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.SelfHost.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Cron;
    using Naos.Deployment.Domain;

    using Spritely.Recipes;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private async Task<List<SetupStep>> GetSelfHostSpecificSteps(InitializationStrategySelfHost selfHostStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string consoleRootPath, string environment, string adminPassword, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            var selfHostSteps = new List<SetupStep>();
            var selfHostDnsEntries = selfHostStrategy.SelfHostSupportedDnsEntries.Select(funcToCreateNewDnsWithTokensReplaced).ToList();
            var sslCertificateName = selfHostStrategy.SslCertificateName;
            var scheduledTaskAccount = this.GetAccountToUse(selfHostStrategy);
            var selfHostExeName = selfHostStrategy.SelfHostExeName;
            var applicationId = Guid.NewGuid().ToString().ToUpper();

            // specific steps to support self hosting
            var certDetails = await this.certificateRetriever.GetCertificateByNameAsync(sslCertificateName);
            if (certDetails == null)
            {
                throw new DeploymentException("Could not find certificate by name: " + sslCertificateName);
            }

            var certificateTargetPath = Path.Combine(consoleRootPath, certDetails.GenerateFileName());
            selfHostSteps.Add(
                new SetupStep
                    {
                        Description = "Send certificate file (removed after installation): " + certDetails.GenerateFileName(),
                        SetupFunc = machineManager =>
                            {
                                machineManager.SendFile(certificateTargetPath, certDetails.PfxBytes);
                                return new dynamic[0];
                            }
                    });

            var configureCertParams = new object[] { certificateTargetPath, certDetails.PfxPasswordInClearText.ToSecureString(), applicationId, selfHostDnsEntries };
            selfHostSteps.Add(
                new SetupStep
                    {
                        Description = $"Configure SSL Certificate {sslCertificateName} for Self Hosting",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(
                                this.settings.DeploymentScriptBlocks.ConfigureSslCertificateForHosting.ScriptText,
                                configureCertParams)
                    });

            var configureUserParams = new object[] { scheduledTaskAccount, selfHostDnsEntries };
            selfHostSteps.Add(
                new SetupStep
                    {
                        Description = $"Configure user {scheduledTaskAccount} for Self Hosting",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(this.settings.DeploymentScriptBlocks.ConfigureUserForHosting.ScriptText, configureUserParams)
                    });

            var openPortParams = new[] { "443", "Allow TCP 443 IN for Self Hosting" };
            selfHostSteps.Add(
                new SetupStep
                    {
                        Description = $"Open port 443 for Self Hosting",
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(this.settings.DeploymentScriptBlocks.OpenPort.ScriptText, openPortParams)
                    });

            // task steps to keep the console exe alive
            var schedule = new IntervalSchedule { Interval = TimeSpan.FromMinutes(1) };
            var exeName = selfHostExeName;
            var name = "SelfHostKeepAliveFor" + exeName;
            var description = $"Task to ensure that the self host {exeName} is always running.";
            var scheduledTaskStesps = this.GetScheduledTaskSpecificStepsParameterizedWithoutStrategy(
                itsConfigOverrides,
                consoleRootPath,
                environment,
                exeName,
                schedule,
                scheduledTaskAccount,
                adminPassword,
                name,
                description,
                null);

            selfHostSteps.AddRange(scheduledTaskStesps);

            return selfHostSteps;
        }

        private string GetAccountToUse(InitializationStrategySelfHost selfHostStrategy)
        {
            var scheduledTaskAccount = string.IsNullOrEmpty(selfHostStrategy.ScheduledTaskAccount)
                                           ? this.settings.HarnessSettings.HarnessAccount
                                           : selfHostStrategy.ScheduledTaskAccount;
            return scheduledTaskAccount;
        }
    }
}
