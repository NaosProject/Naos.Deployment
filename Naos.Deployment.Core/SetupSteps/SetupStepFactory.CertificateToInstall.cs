// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.CertificateToInstall.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.IO;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public partial class SetupStepFactory
    {
        private List<SetupStep> GetCertificateToInstallSpecificSteps(InitializationStrategyCertificateToInstall certToInstallStrategy, string packageDirectoryPath, string harnessAccount, string iisAccount)
        {
            var certSteps = new List<SetupStep>();

            var userToGrantPrivateKeyAccess = certToInstallStrategy.UserToGrantPrivateKeyAccess;
            var tokenAppliedUser = TokenSubstitutions.GetSubstitutedStringForAccounts(
                userToGrantPrivateKeyAccess,
                harnessAccount,
                iisAccount);

            var certificateName = certToInstallStrategy.CertificateToInstall;

            var certDetails = this.certificateRetriever.GetCertificateByName(certificateName);
            if (certDetails == null)
            {
                throw new DeploymentException("Could not find certificate by name: " + certificateName);
            }

            var certificateTargetPath = Path.Combine(packageDirectoryPath, certDetails.GenerateFileName());
            certSteps.Add(
                new SetupStep
                {
                    Description =
                        "Send certificate file (removed after installation): "
                        + certDetails.GenerateFileName(),
                    SetupAction =
                        machineManager =>
                        machineManager.SendFile(certificateTargetPath, certDetails.FileBytes)
                });

            var installCertificateParams = new object[] { certificateTargetPath, certDetails.CertificatePassword, tokenAppliedUser };

            certSteps.Add(
                new SetupStep
                {
                    Description = "Installing certificate: " + certificateName,
                    SetupAction =
                        machineManager =>
                        machineManager.RunScript(
                            this.settings.DeploymentScriptBlocks.InstallCertificate.ScriptText,
                            installCertificateParams)
                });

            return certSteps;
        }
    }
}
