// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.CertificateToInstall.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Naos.Deployment.Domain;

    using Spritely.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    internal partial class SetupStepFactory
    {
        private async Task<List<SetupStep>> GetCertificateToInstallSpecificStepsAsync(InitializationStrategyCertificateToInstall certToInstallStrategy, string packageId, string packageDirectoryPath, string harnessAccount, string iisAccount)
        {
            var usersToGrantPrivateKeyAccess = new[] { certToInstallStrategy.AccountToGrantPrivateKeyAccess };
            var certificateName = certToInstallStrategy.CertificateToInstall;
            var installExportable = certToInstallStrategy.InstallExportable;

            return await this.GetCertificateToInstallSpecificStepsParameterizedWithoutStrategyAsync(packageId, packageDirectoryPath, harnessAccount, iisAccount, usersToGrantPrivateKeyAccess, certificateName, installExportable);
        }

        private async Task<List<SetupStep>> GetCertificateToInstallSpecificStepsParameterizedWithoutStrategyAsync(string packageId, string tempPathToStoreFileWhileInstalling, string harnessAccount, string iisAccount, ICollection<string> usersToGrantPrivateKeyAccess, string certificateName, bool installExportable)
        {
            var certSteps = new List<SetupStep>();

            var tokenAppliedUsers =
                usersToGrantPrivateKeyAccess.Select(_ => TokenSubstitutions.GetSubstitutedStringForAccounts(_, harnessAccount, iisAccount)).ToArray();
            var tokenAppliedUsersString = string.Join(",", tokenAppliedUsers);

            var certDetails = await this.certificateRetriever.GetCertificateByNameAsync(certificateName);
            if (certDetails == null)
            {
                throw new DeploymentException(Invariant($"Could not find certificate by name '{certificateName}' for '{packageId}'"));
            }

            var certificateTargetPath = Path.Combine(tempPathToStoreFileWhileInstalling, certDetails.GenerateFileName());
            certSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"Send certificate file (removed after installation) '{certDetails.GenerateFileName()}' for '{packageId}.'"),
                        SetupFunc = machineManager =>
                            {
                                machineManager.SendFile(certificateTargetPath, certDetails.PfxBytes);
                                return new dynamic[0];
                            },
                    });

            var installCertificateParams = new object[] { certificateTargetPath, certDetails.PfxPasswordInClearText.ToSecureString(), installExportable, tokenAppliedUsers };

            certSteps.Add(
                new SetupStep
                    {
                        Description = Invariant($"Installing certificate  '{certificateName}' for [{tokenAppliedUsersString}] for '{packageId}'."),
                        SetupFunc =
                            machineManager =>
                            machineManager.RunScript(this.Settings.DeploymentScriptBlocks.InstallCertificate.ScriptText, installCertificateParams),
                    });

            return certSteps;
        }
    }
}
