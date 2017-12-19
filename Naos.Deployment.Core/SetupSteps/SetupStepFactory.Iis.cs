// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.Iis.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private async Task<List<SetupStep>> GetIisSpecificSetupStepsAsync(InitializationStrategyIis iisStrategy, IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides, string webRootPath, string environment, string adminPassword, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
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

            var appPoolStartMode = iisStrategy.AppPoolStartMode == ApplicationPoolStartMode.None
                                       ? ApplicationPoolStartMode.OnDemand
                                       : iisStrategy.AppPoolStartMode;

            var appPoolAccount = this.GetAccountToUse(iisStrategy);

            var autoStartProviderName = iisStrategy.AutoStartProvider?.Name;
            var autoStartProviderType = iisStrategy.AutoStartProvider?.Type;

            var hostHeadersForHttpsBinding = (iisStrategy.HostHeadersForHttpsBinding ?? new List<string>()).ToList();
            var hostHeaderForHttpBinding = iisStrategy.HostHeaderForHttpBinding;

            const bool EnableSni = false;

            var appPoolPassword = appPoolAccount == null
                                      ? null
                                      : appPoolAccount.ToUpperInvariant()
                                        == this.AdministratorAccount.ToUpperInvariant()
                                            ? adminPassword
                                            : null;

            var installWebParameters = new object[]
                                           {
                                               webRootPath, primaryDns, StoreLocation.LocalMachine.ToString(), StoreName.My.ToString(),
                                               certDetails.GetPowershellPathableThumbprint(), appPoolAccount, appPoolPassword, appPoolStartMode,
                                               autoStartProviderName, autoStartProviderType, EnableSni, hostHeadersForHttpsBinding,
                                               hostHeaderForHttpBinding,
                                           };

            webSteps.Add(
                new SetupStep
                {
                    Description = "Install IIS and configure website/webservice.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(this.Settings.DeploymentScriptBlocks.InstallAndConfigureWebsite.ScriptText, installWebParameters),
                });

            return webSteps;
        }

        private string GetAccountToUse(InitializationStrategyIis iisStrategy)
        {
            var appPoolAccount = string.IsNullOrEmpty(iisStrategy.AppPoolAccount) ? this.Settings.WebServerSettings.IisAccount : iisStrategy.AppPoolAccount;
            return appPoolAccount;
        }
    }
}
