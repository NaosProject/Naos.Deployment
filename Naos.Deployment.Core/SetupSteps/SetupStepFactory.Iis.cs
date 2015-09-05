// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.Iis.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private List<SetupStep> GetIisSpecificSetupSteps(InitializationStrategyIis iisStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string packageDirectoryPath, string webRootPath, string environment)
        {
            var webSteps = new List<SetupStep>();

            var webConfigPath = Path.Combine(webRootPath, "web.config");
            var updateWebConfigScriptBlock = this.settings.DeploymentScriptBlocks.UpdateItsConfigPrecedence;
            var updateWebConfigScriptParams = new[] { webConfigPath, environment };

            webSteps.Add(
                new SetupStep
                {
                    Description = "Update Its.Config precedence: " + environment,
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(
                            updateWebConfigScriptBlock.ScriptText,
                            updateWebConfigScriptParams)
                });

            foreach (var itsConfigOverride in itsConfigOverrides ?? new List<ItsConfigOverride>())
            {
                var itsFileSubPath = string.Format(
                    ".config/{0}/{1}.json",
                    environment,
                    itsConfigOverride.FileNameWithoutExtension);

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

            var certDetails = this.certificateRetriever.GetCertificateByName(iisStrategy.SslCertificateName);
            if (certDetails == null)
            {
                throw new DeploymentException("Could not find certificate by name: " + iisStrategy.SslCertificateName);
            }

            var certificateTargetPath = Path.Combine(packageDirectoryPath, certDetails.GenerateFileName());
            var appPoolStartMode = iisStrategy.AppPoolStartMode == ApplicationPoolStartMode.None
                                       ? ApplicationPoolStartMode.OnDemand
                                       : iisStrategy.AppPoolStartMode;

            var autoStartProviderName = iisStrategy.AutoStartProvider == null
                                            ? null
                                            : iisStrategy.AutoStartProvider.Name;
            var autoStartProviderType = iisStrategy.AutoStartProvider == null
                                            ? null
                                            : iisStrategy.AutoStartProvider.Type;

            const bool EnableSni = false;
            const bool AddHostHeaders = true;
            var installWebParameters = new object[]
                                           {
                                               webRootPath, iisStrategy.PrimaryDns, certificateTargetPath,
                                               certDetails.CertificatePassword, appPoolStartMode, autoStartProviderName,
                                               autoStartProviderType, EnableSni, AddHostHeaders
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
