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
    using System.Threading.Tasks;

    using Naos.Cron;
    using Naos.Deployment.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private async Task<List<SetupStep>> GetSelfHostSpecificSteps(InitializationStrategySelfHost selfHostStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string consoleRootPath, string environment, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            var selfHostSteps = new List<SetupStep>();
            var selfHostDns = funcToCreateNewDnsWithTokensReplaced(selfHostStrategy.SelfHostDns);
            var sslCertificateName = selfHostStrategy.SslCertificateName;
            var scheduledTaskAccount = selfHostStrategy.ScheduledTaskAccount;
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
                    SetupAction =
                        machineManager => machineManager.SendFile(certificateTargetPath, certDetails.FileBytes)
                });

            var configureCertParams = new object[] { certificateTargetPath, certDetails.CertificatePassword, applicationId, selfHostDns };
            selfHostSteps.Add(
                new SetupStep
                    {
                        Description = $"Configure SSL Certificate {sslCertificateName} for Self Hosting",
                        SetupAction =
                            machineManager =>
                            machineManager.RunScript(
                                this.settings.DeploymentScriptBlocks.ConfigureSslCertificateForHosting.ScriptText,
                                configureCertParams)
                    });

            var account = scheduledTaskAccount ?? this.settings.HarnessSettings.HarnessAccount;
            var configureUserParams = new[] { account, selfHostDns };
            selfHostSteps.Add(
                new SetupStep
                    {
                        Description = $"Configure user {account} for Self Hosting",
                        SetupAction =
                            machineManager =>
                            machineManager.RunScript(this.settings.DeploymentScriptBlocks.ConfigureUserForHosting.ScriptText, configureUserParams)
                    });

            var openPortParams = new[] { "443", "Allow TCP 443 IN for Self Hosting" };
            selfHostSteps.Add(
                new SetupStep
                    {
                        Description = $"Open port 443 for Self Hosting",
                        SetupAction =
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
                name,
                description,
                null);

            selfHostSteps.AddRange(scheduledTaskStesps);

            return selfHostSteps;
        }
    }
}
