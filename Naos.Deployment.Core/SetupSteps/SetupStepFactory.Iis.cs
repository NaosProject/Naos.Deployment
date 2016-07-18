// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.Iis.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private async Task<List<SetupStep>> GetIisSpecificSetupStepsAsync(InitializationStrategyIis iisStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string packageDirectoryPath, string webRootPath, string environment, string adminPassword, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            var primaryDns = funcToCreateNewDnsWithTokensReplaced(iisStrategy.PrimaryDns);

            var webSteps = new List<SetupStep>();
            var webConfigPath = Path.Combine(webRootPath, "web.config");

            var itsConfigSteps = this.GetItsConfigSteps(itsConfigOverrides, webRootPath, environment, webConfigPath);
            webSteps.AddRange(itsConfigSteps);

            var certDetails = await this.certificateRetriever.GetCertificateByNameAsync(iisStrategy.SslCertificateName);
            if (certDetails == null)
            {
                throw new DeploymentException("Could not find certificate by name: " + iisStrategy.SslCertificateName);
            }

            var certificateTargetPath = Path.Combine(packageDirectoryPath, certDetails.GenerateFileName());
            var appPoolStartMode = iisStrategy.AppPoolStartMode == ApplicationPoolStartMode.None
                                       ? ApplicationPoolStartMode.OnDemand
                                       : iisStrategy.AppPoolStartMode;

            var appPoolAccount = this.GetAccountToUse(iisStrategy);

            var autoStartProviderName = iisStrategy.AutoStartProvider == null
                                            ? null
                                            : iisStrategy.AutoStartProvider.Name;
            var autoStartProviderType = iisStrategy.AutoStartProvider == null
                                            ? null
                                            : iisStrategy.AutoStartProvider.Type;

            var enableHttp = iisStrategy.EnableHttp;

            const bool EnableSni = false;
            const bool AddHostHeaders = true;

            var appPoolPassword = appPoolAccount == null
                                      ? null
                                      : appPoolAccount.ToUpperInvariant()
                                        == this.AdministratorAccount.ToUpperInvariant()
                                            ? adminPassword
                                            : null;

            var installWebParameters = new object[]
                                           {
                                               webRootPath, primaryDns, certificateTargetPath,
                                               certDetails.CertificatePassword, appPoolAccount, appPoolPassword, appPoolStartMode, autoStartProviderName,
                                               autoStartProviderType, EnableSni, AddHostHeaders, enableHttp
                                           };

            webSteps.Add(
                new SetupStep
                    {
                        Description = "Send certificate file (removed after installation): " + certDetails.GenerateFileName(),
                        SetupFunc = machineManager =>
                            {
                                machineManager.SendFile(certificateTargetPath, certDetails.FileBytes);
                                return new dynamic[0];
                            }
                    });

            webSteps.Add(
                new SetupStep
                {
                    Description = "Install IIS and configure website/webservice.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(this.settings.DeploymentScriptBlocks.InstallAndConfigureWebsite.ScriptText, installWebParameters)
                });

            return webSteps;
        }

        private string GetAccountToUse(InitializationStrategyIis iisStrategy)
        {
            var appPoolAccount = string.IsNullOrEmpty(iisStrategy.AppPoolAccount) ? this.settings.WebServerSettings.IisAccount : iisStrategy.AppPoolAccount;
            return appPoolAccount;
        }
    }
}
