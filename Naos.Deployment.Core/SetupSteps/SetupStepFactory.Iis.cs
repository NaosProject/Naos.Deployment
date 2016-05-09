// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.Iis.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private async Task<List<SetupStep>> GetIisSpecificSetupStepsAsync(InitializationStrategyIis iisStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string packageDirectoryPath, string webRootPath, string environment, string adminPassword)
        {
            var webSteps = new List<SetupStep>();

            var webConfigPath = Path.Combine(webRootPath, "web.config");
            var updateWebConfigScriptBlock = this.settings.DeploymentScriptBlocks.UpdateItsConfigPrecedence;
            var precedenceChain = new[] { environment }.ToList();
            precedenceChain.AddRange(this.itsConfigPrecedenceAfterEnvironment);
            var updateWebConfigScriptParams = new object[] { webConfigPath, precedenceChain.ToArray() };

            webSteps.Add(
                new SetupStep
                {
                    Description = "Update Its.Config precedence: " + string.Join("|", precedenceChain),
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(
                            updateWebConfigScriptBlock.ScriptText,
                            updateWebConfigScriptParams)
                });

            foreach (var itsConfigOverride in itsConfigOverrides ?? new List<ItsConfigOverride>())
            {
                var itsFileSubPath = $".config/{environment}/{itsConfigOverride.FileNameWithoutExtension}.json";

                var itsFilePath = Path.Combine(webRootPath, itsFileSubPath);
                var itsFileBytes = Encoding.UTF8.GetBytes(itsConfigOverride.FileContentsJson);

                webSteps.Add(
                    new SetupStep
                    {
                        Description =
                            "(Over)write Its.Config file: " + itsConfigOverride.FileNameWithoutExtension,
                        SetupAction =
                            machineManager => machineManager.SendFile(itsFilePath, itsFileBytes, false, true)
                    });
            }

            var certDetails = await this.certificateRetriever.GetCertificateByNameAsync(iisStrategy.SslCertificateName);
            if (certDetails == null)
            {
                throw new DeploymentException("Could not find certificate by name: " + iisStrategy.SslCertificateName);
            }

            var certificateTargetPath = Path.Combine(packageDirectoryPath, certDetails.GenerateFileName());
            var appPoolStartMode = iisStrategy.AppPoolStartMode == ApplicationPoolStartMode.None
                                       ? ApplicationPoolStartMode.OnDemand
                                       : iisStrategy.AppPoolStartMode;

            var appPoolAccount = iisStrategy.AppPoolAccount;

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
                                               webRootPath, iisStrategy.PrimaryDns, certificateTargetPath,
                                               certDetails.CertificatePassword, appPoolAccount, appPoolPassword, appPoolStartMode, autoStartProviderName,
                                               autoStartProviderType, EnableSni, AddHostHeaders, enableHttp
                                           };

            webSteps.Add(
                new SetupStep
                {
                    Description = "Send certificate file (removed after installation): " + certDetails.GenerateFileName(),
                    SetupAction =
                        machineManager => machineManager.SendFile(certificateTargetPath, certDetails.FileBytes)
                });

            webSteps.Add(
                new SetupStep
                {
                    Description = "Install IIS and configure website/webservice (this could take several minutes).",
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(this.settings.DeploymentScriptBlocks.InstallAndConfigureWebsite.ScriptText, installWebParameters)
                });

            return webSteps;
        }
    }
}
