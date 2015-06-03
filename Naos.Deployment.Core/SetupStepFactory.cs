// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactory.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Factory to create a list of setup steps from various situations (abstraction to actual machine setup).
    /// </summary>
    public class SetupStepFactory
    {
        /// <summary>
        /// Root path that all packages are deployed to.
        /// </summary>
        public const string RootDeploymentPath = @"D:\Deployments\";

        private readonly IGetCertificates certificateManager;

        public SetupStepFactory(IGetCertificates certificateManager)
        {
            this.certificateManager = certificateManager;
        }

        /// <summary>
        /// Get the appropriate setup steps for the packaged config.
        /// </summary>
        /// <param name="packagedConfig">Config to base setup steps from.</param>
        /// <param name="environment">Environment that is being deployed.</param>
        /// <returns>Collection of setup steps that will leave the machine properly configured.</returns>
        public ICollection<SetupStep> GetSetupSteps(PackagedDeploymentConfiguration packagedConfig, string environment)
        {
            var ret = new List<SetupStep>();

            var installChocoStep = GetChocolateySetupStep(packagedConfig);
            if (installChocoStep != null)
            {
                ret.Add(installChocoStep);
            }

            var deployUnzippedFileStep = GetCopyAndUnzipPackageStep(packagedConfig);
            ret.Add(deployUnzippedFileStep);

            foreach (var initializationStrategy in packagedConfig.InitializationStrategies)
            {
                var initSteps = this.GetStrategySpecificSetupSteps(initializationStrategy, packagedConfig, environment);
                ret.AddRange(initSteps);
            }

            return ret;
        }

        private ICollection<SetupStep> GetStrategySpecificSetupSteps(InitializationStrategyBase strategy, PackagedDeploymentConfiguration packagedConfig, string environment)
        {
            var ret = new List<SetupStep>();
            var packageDirectoryPath = GetPackageDirectoryPath(packagedConfig);

            if (strategy.GetType() == typeof(InitializationStrategyWeb))
            {
                var webRootPath = Path.Combine(packageDirectoryPath, "packagedWebsite"); // this needs to match how the package was built in the build system...
                var webSteps = this.GetWebSpecificSetupSteps(
                    (InitializationStrategyWeb)strategy,
                    packagedConfig.ItsConfigOverrides,
                    packageDirectoryPath,
                    webRootPath,
                    environment);
                ret.AddRange(webSteps);
            }
            else if (packagedConfig.GetType() == typeof(InitializationStrategyMessageBusHandler))
            {
                /* No additional steps necessary as the DeploymentManager should have included a harness by virtue of this type of initialization strategy */
            }
            else if (packagedConfig.GetType() == typeof(InitializationStrategyDatabase))
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotSupportedException("The initialization strategy type is not supported: " + packagedConfig.GetType());
            }

            return ret;
        }

        private List<SetupStep> GetWebSpecificSetupSteps(InitializationStrategyWeb webStrategy, ICollection<ItsConfigOverride> itsConfigOverrides, string packageDirectoryPath, string webRootPath, string environment)
        {
            var webSteps = new List<SetupStep>();

            var webConfigPath = Path.Combine(webRootPath, "web.config");
            var updateWebConfigScriptBlock = InstallScriptBlocks.UpdateItsConfigPrecedence;
            var updateWebConfigScriptParams = new[] { webConfigPath, environment };

            webSteps.Add(
                new SetupStep()
                    {
                        Description = "Update Its.Config precedence",
                        SetupAction =
                            (machineManager) =>
                            machineManager.RunScript(updateWebConfigScriptBlock, updateWebConfigScriptParams)
                    });

            foreach (var itsConfigOverride in itsConfigOverrides ?? new List<ItsConfigOverride>())
            {
                var itsFileSubPath = string.Format(
                    ".config/{0}/{1}.json",
                    environment,
                    itsConfigOverride.FileNameWithoutExtension);

                var itsFilePath = Path.Combine(webRootPath, itsFileSubPath);
                var itsFileBytes = Encoding.ASCII.GetBytes(itsConfigOverride.FileContentsJson);

                webSteps.Add(
                    new SetupStep()
                        {
                            Description =
                                "Write Its.Config file - " + itsConfigOverride.FileNameWithoutExtension,
                            SetupAction =
                                (machineManager) => machineManager.SendFile(itsFilePath, itsFileBytes)
                        });
            }

            var certDetails = this.certificateManager.GetCertificateByName(webStrategy.SslCertificateName);
            if (certDetails == null)
            {
                throw new DeploymentException("Could not find certificate by name: " + webStrategy.SslCertificateName);
            }

            var certificateTargetPath = Path.Combine(packageDirectoryPath, certDetails.GenerateFileName());
            var appPoolStartMode = webStrategy.AppPoolStartMode == ApplicationPoolStartMode.None
                                       ? ApplicationPoolStartMode.OnDemand
                                       : webStrategy.AppPoolStartMode;

            var autoStartProviderName = webStrategy.AutoStartProvider == null
                                            ? null
                                            : webStrategy.AutoStartProvider.Name;
            var autoStartProviderType = webStrategy.AutoStartProvider == null
                                            ? null
                                            : webStrategy.AutoStartProvider.Type;

            var enableSni = false;
            var addHostHeaders = true;
            var installWebParameters = new object[]
                                           {
                                               webRootPath, webStrategy.PrimaryDns, certificateTargetPath,
                                               certDetails.CertificatePassword, appPoolStartMode, autoStartProviderName,
                                               autoStartProviderType, enableSni, addHostHeaders,
                                           };

            webSteps.Add(
                new SetupStep()
                    {
                        Description = "Send certificate file",
                        SetupAction =
                            (machineManager) => machineManager.SendFile(certificateTargetPath, certDetails.FileBytes)
                    });

            webSteps.Add(
                new SetupStep()
                    {
                        Description = "Install IIS and tools",
                        SetupAction =
                            (machineManager) =>
                            machineManager.RunScript(InstallScriptBlocks.InstallWeb, installWebParameters)
                    });

            return webSteps;
        }

        private static SetupStep GetCopyAndUnzipPackageStep(PackagedDeploymentConfiguration packagedConfig)
        {
            var packageDirectoryPath = GetPackageDirectoryPath(packagedConfig);
            var packageFilePath = Path.Combine(packageDirectoryPath, "Package.zip");
            var unzipScript = InstallScriptBlocks.UnzipFile;
            var unzipParams = new[] { packageFilePath, packageDirectoryPath };
            var deployUnzippedFileStep = new SetupStep
                                                   {
                                                       Description = "Push package file and unzip.",
                                                       SetupAction = (machineManager) =>
                                                           {
                                                               machineManager.SendFile(
                                                                   packageFilePath,
                                                                   packagedConfig.Package.PackageFileBytes);
                                                               machineManager.RunScript(unzipScript, unzipParams);
                                                           }
                                                   };

            return deployUnzippedFileStep;
        }

        private static SetupStep GetChocolateySetupStep(PackagedDeploymentConfiguration packagedConfig)
        {
            SetupStep installChocoStep = null;
            if (packagedConfig.DeploymentConfiguration.ChocolateyPackages != null
                && packagedConfig.DeploymentConfiguration.ChocolateyPackages.Any())
            {
                installChocoStep = new SetupStep
                {
                    Description =
                        "Install Chocolatey Packages; IDs: "
                        + string.Join(
                            ",",
                            packagedConfig.DeploymentConfiguration.ChocolateyPackages
                              .Select(_ => _.Id)),
                    SetupAction = (machineManager) =>
                    {
                        var installChocolateyPackagesLines =
                            packagedConfig.DeploymentConfiguration.ChocolateyPackages
                                .Select(
                                    _ =>
                                    _.Version == null
                                        ? "choco install " + _.Id + " -y"
                                        : "choco install " + _.Id + " -Version "
                                          + _.Version + " -y");
                        var installChocolateyPackagesScriptBlock = "{"
                                                                   + string.Join(
                                                                       Environment
                                                                         .NewLine,
                                                                       installChocolateyPackagesLines)
                                                                   + "}";
                        machineManager.RunScript(
                            installChocolateyPackagesScriptBlock,
                            new object[0]);
                    }
                };
            }

            return installChocoStep;
        }

        private static string GetPackageDirectoryPath(PackagedDeploymentConfiguration packagedConfig)
        {
            return Path.Combine(RootDeploymentPath, packagedConfig.Package.PackageDescription.Id);
        }
    }
}
