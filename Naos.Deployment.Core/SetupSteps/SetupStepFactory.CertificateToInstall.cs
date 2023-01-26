// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.CertificateToInstall.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
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
    public partial class SetupStepFactory
    {
        private async Task<List<SetupStep>> GetCertificateToInstallSpecificStepsAsync(InitializationStrategyCertificateToInstall certToInstallStrategy, string packageId, string packageDirectoryPath, Func<string, string> funcToReplaceTokensInReplacementValue)
        {
            var usersToGrantPrivateKeyAccess = new[] { certToInstallStrategy.AccountToGrantPrivateKeyAccess };
            var certificateName = certToInstallStrategy.CertificateToInstall;
            var installExportable = certToInstallStrategy.InstallExportable;

            return await this.GetCertificateToInstallSpecificStepsParameterizedWithoutStrategyAsync(packageId, packageDirectoryPath, funcToReplaceTokensInReplacementValue, usersToGrantPrivateKeyAccess, certificateName, installExportable);
        }

        private async Task<List<SetupStep>> GetCertificateToInstallSpecificStepsParameterizedWithoutStrategyAsync(string packageId, string tempPathToStoreFileWhileInstalling, Func<string, string> funcToReplaceTokensInReplacementValue, ICollection<string> usersToGrantPrivateKeyAccess, string certificateName, bool installExportable)
        {
            var certSteps = new List<SetupStep>();

            var tokenAppliedUsers = usersToGrantPrivateKeyAccess.Select(funcToReplaceTokensInReplacementValue).ToArray();
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
                            machineManager.RunScript(this.Settings.DeploymentScriptBlocks.InstallCertificate.ScriptText, installCertificateParams).ToList(),
                    });

            return certSteps;
        }
    }
}
