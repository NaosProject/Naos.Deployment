// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.Iis.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
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
    using Naos.Logging.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private List<SetupStep> GetIisSpecificSetupSteps(InitializationStrategyIis iisStrategy, LogWritingSettings defaultLogWritingSettings, IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides, string webRootPath, string environment, string adminPassword, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            // We are no longer adding HTTP bindings and thus not checking for the existence of the supporting certs.
            // The certificate installation process will create the bindings.
            var httpsBindingDefinitions = iisStrategy.HttpsBindings ?? new HttpsBinding[0];

            var primaryDns = funcToCreateNewDnsWithTokensReplaced(iisStrategy.PrimaryDns);

            var webSteps = new List<SetupStep>();
            var webConfigPath = Path.Combine(webRootPath, "web.config");

            var itsConfigSteps = this.GetItsConfigSteps(itsConfigOverrides, defaultLogWritingSettings, webRootPath, environment, webConfigPath);
            webSteps.AddRange(itsConfigSteps);

            var certificateNameToThumbprintMap = new Dictionary<string, string>();

            var httpsBindings = httpsBindingDefinitions
                .Select(_ => new { Thumbprint = certificateNameToThumbprintMap[_.SslCertificateName], HostHeader = _.HostHeader }).ToList();

            var appPoolStartMode = iisStrategy.AppPoolStartMode == ApplicationPoolStartMode.None
                                       ? ApplicationPoolStartMode.OnDemand
                                       : iisStrategy.AppPoolStartMode;

            var appPoolAccount = this.GetAccountToUse(iisStrategy);

            var autoStartProviderName = iisStrategy.AutoStartProvider?.Name;
            var autoStartProviderType = iisStrategy.AutoStartProvider?.Type;

            var hostHeaderForHttpBinding = iisStrategy.HostHeaderForHttpBinding;

            var appPoolPassword = appPoolAccount == null
                                      ? null
                                      : appPoolAccount.ToUpperInvariant()
                                        == this.AdministratorAccount.ToUpperInvariant()
                                            ? adminPassword
                                            : null;

            var configureWebsiteParameters = new object[]
                                           {
                                               webRootPath,
                                               primaryDns,
                                               StoreLocation.LocalMachine.ToString(),
                                               StoreName.My.ToString(),
                                               appPoolAccount,
                                               appPoolPassword,
                                               appPoolStartMode,
                                               autoStartProviderName,
                                               autoStartProviderType,
                                               iisStrategy.EnableSni,
                                               httpsBindings,
                                               hostHeaderForHttpBinding,
                                               iisStrategy.CertificateHostNames.ToList(),
                                               iisStrategy.AcmeClientRoute53DnsChallengeHandlerAccessKey,
                                               iisStrategy.AcmeClientRoute53DnsChallengeHandlerSecretKey,
                                           };

            webSteps.Add(
                new SetupStep
                {
                    Description = "Configure website/webservice in IIS.",
                    SetupFunc =
                        machineManager =>
                        machineManager.RunScript(this.Settings.DeploymentScriptBlocks.ConfigureIis.ScriptText, configureWebsiteParameters).ToList(),
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
