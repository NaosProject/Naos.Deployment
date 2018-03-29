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
    using Naos.Logging.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private async Task<List<SetupStep>> GetIisSpecificSetupStepsAsync(InitializationStrategyIis iisStrategy, LogProcessorSettings defaultLogProcessorSettings, IReadOnlyCollection<ItsConfigOverride> itsConfigOverrides, string webRootPath, string environment, string adminPassword, Func<string, string> funcToCreateNewDnsWithTokensReplaced)
        {
            var httpsBindingDefinitions = iisStrategy.HttpsBindings ?? new HttpsBinding[0];
            if (httpsBindingDefinitions.Where(_ => string.IsNullOrWhiteSpace(_.HostHeader)).ToList().Count > 1)
            {
                throw new ArgumentException(Invariant($"Cannot have more than one binding without a {nameof(HttpsBinding)}.{nameof(HttpsBinding.HostHeader)} that is blank; site {iisStrategy.PrimaryDns}."));
            }

            if (httpsBindingDefinitions.Count == 0 && string.IsNullOrWhiteSpace(iisStrategy.HostHeaderForHttpBinding))
            {
                throw new ArgumentException(Invariant($"Must specify {nameof(iisStrategy.HttpsBindings)} and/or {iisStrategy.HostHeaderForHttpBinding}; site {iisStrategy.PrimaryDns}."));
            }

            if (httpsBindingDefinitions.Any(_ => string.IsNullOrWhiteSpace(_.SslCertificateName)))
            {
                throw new ArgumentException(Invariant($"Must specify specify a {nameof(HttpsBinding.SslCertificateName)} on all {nameof(iisStrategy.HttpsBindings)}; site {iisStrategy.PrimaryDns}."));
            }

            var primaryDns = funcToCreateNewDnsWithTokensReplaced(iisStrategy.PrimaryDns);

            var webSteps = new List<SetupStep>();
            var webConfigPath = Path.Combine(webRootPath, "web.config");

            var itsConfigSteps = this.GetItsConfigSteps(itsConfigOverrides, defaultLogProcessorSettings, webRootPath, environment, webConfigPath);
            webSteps.AddRange(itsConfigSteps);

            var certificateNameToThumbprintMap = new Dictionary<string, string>();
            foreach (var bindingDefinition in httpsBindingDefinitions)
            {
                if (!certificateNameToThumbprintMap.ContainsKey(bindingDefinition.SslCertificateName))
                {
                    var certDetails = await this.certificateRetriever.GetCertificateByNameAsync(bindingDefinition.SslCertificateName);
                    if (certDetails == null)
                    {
                        throw new DeploymentException("Could not find certificate by name: " + bindingDefinition.SslCertificateName);
                    }

                    certificateNameToThumbprintMap.Add(bindingDefinition.SslCertificateName, certDetails.GetPowershellPathableThumbprint());
                }
            }

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

            var installWebParameters = new object[]
                                           {
                                               webRootPath, primaryDns, StoreLocation.LocalMachine.ToString(), StoreName.My.ToString(), appPoolAccount,
                                               appPoolPassword, appPoolStartMode, autoStartProviderName, autoStartProviderType, iisStrategy.EnableSni,
                                               httpsBindings, hostHeaderForHttpBinding,
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
