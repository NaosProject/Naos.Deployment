// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagedDeploymentConfigurationExtensionMethods.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Additional behavior to add the initialization strategies.
    /// </summary>
    public static class PackagedDeploymentConfigurationExtensionMethods
    {
        public static ICollection<PackagedDeploymentConfiguration> ApplyDefaults(
            this ICollection<PackagedDeploymentConfiguration> packagedConfigs,
            DeploymentConfiguration defaultDeploymentConfig)
        {
            if (packagedConfigs.Count == 0)
            {
                return
                    new[]
                        {
                            new PackagedDeploymentConfiguration
                                {
                                    Package = null,
                                    DeploymentConfiguration = defaultDeploymentConfig
                                }
                        }
                        .ToList();
            }
            else
            {
                return
                    packagedConfigs.Select(
                        _ =>
                        new PackagedDeploymentConfiguration
                            {
                                Package = _.Package,
                                DeploymentConfiguration =
                                    _.DeploymentConfiguration.ApplyDefaults(
                                        defaultDeploymentConfig)
                            }).ToList();
            }
        }

        /// <summary>
        /// Overrides the deployment config in a collection of packaged configurations.
        /// </summary>
        /// <param name="baseCollection">Base collection of packaged configurations to operate on.</param>
        /// <param name="overrideConfig">Configuration to apply as an override.</param>
        /// <returns>New collection of packaged configurations with overrides applied.</returns>
        public static ICollection<PackagedDeploymentConfiguration>
            OverrideDeploymentConfigAndMergeInitializationStrategies(
            this ICollection<PackagedDeploymentConfiguration> baseCollection,
            DeploymentConfiguration overrideConfig)
        {
            var ret = baseCollection.Select(
                _ =>
                new PackagedDeploymentConfiguration
                    {
                        DeploymentConfiguration =
                            new DeploymentConfiguration
                                {
                                    ChocolateyPackages =
                                        overrideConfig
                                        .ChocolateyPackages,
                                    InstanceAccessibility =
                                        overrideConfig
                                        .InstanceAccessibility,
                                    InstanceType =
                                        overrideConfig
                                        .InstanceType,
                                    Volumes =
                                        overrideConfig
                                        .Volumes,
                                    InitializationStrategies
                                        =
                                        overrideConfig
                                        .InitializationStrategies
                                        .Concat(
                                            _
                                        .DeploymentConfiguration
                                        .InitializationStrategies)
                                        .ToList(),
                                },
                        Package = _.Package
                    }).ToList();
            return
                ret;
        }

        /// <summary>
        /// Get the appropriate setup steps for the packaged config.
        /// </summary>
        /// <param name="packagedConfig">Config to base setup steps from.</param>
        /// <param name="certificateManager">Certificate manager to handle dependencies on certain types of setup steps.</param>
        /// <param name="environment">Environment that is being deployed.</param>
        /// <returns>Collection of setup steps that will leave the machine properly configured.</returns>
        public static ICollection<SetupStep> GetSetupSteps(this PackagedDeploymentConfiguration packagedConfig, IGetCertificates certificateManager, string environment)
        {
            return
                packagedConfig.DeploymentConfiguration.InitializationStrategies.SelectMany(
                    _ => _.GetSetupSteps(certificateManager, packagedConfig, environment)).ToList();
        }

        private static ICollection<SetupStep> GetSetupSteps(this InitializationStrategy strategy, IGetCertificates certificateManager, PackagedDeploymentConfiguration packagedConfig, string environment)
        {
            var ret = new List<SetupStep>();

            if (packagedConfig.DeploymentConfiguration.ChocolateyPackages != null
                && packagedConfig.DeploymentConfiguration.ChocolateyPackages.Any())
            {
                var installChocosStep = new SetupStep
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
                                packagedConfig.DeploymentConfiguration.ChocolateyPackages.Select(
                                    _ =>
                                    _.Version == null
                                        ? "choco install " + _.Id + " -y"
                                        : "choco install " + _.Id + " -Version " + _.Version + " -y");
                            var installChocolateyPackagesScriptBlock = "{"
                                                                       + string.Join(
                                                                           Environment.NewLine,
                                                                           installChocolateyPackagesLines) + "}";
                            machineManager.RunScript(installChocolateyPackagesScriptBlock, new object[0]);
                        }
                };

                ret.Add(installChocosStep);
            }

            var packageDirectoryPath = Path.Combine(@"D:\Deployments\", packagedConfig.Package.PackageDescription.Id);
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

            ret.Add(deployUnzippedFileStep);

            if (strategy.GetType() == typeof(InitializationStrategyWeb))
            {
                var webConfigPath = Path.Combine(Path.Combine(packageDirectoryPath, "packagedWebsite"), "web.config");
                var updateWebConfigScriptBlock = InstallScriptBlocks.UpdateItsConfigPrecedence;
                var updateWebConfigScriptParams = new[] { webConfigPath, environment };

                ret.Add(
                    new SetupStep()
                        {
                            Description = "Update Its.Config precedence",
                            SetupAction =
                                (machineManager) =>
                                machineManager.RunScript(
                                    updateWebConfigScriptBlock,
                                    updateWebConfigScriptParams)
                        });

                var webStrategy = (InitializationStrategyWeb)strategy;

                var certDetails = certificateManager.GetCertificateByName(webStrategy.SslCertificateName);
                if (certDetails == null)
                {
                    throw new DeploymentException(
                        "Could not find certificate by name: " + webStrategy.SslCertificateName);
                }

                var certificateTargetPath = Path.Combine(packageDirectoryPath, certDetails.GenerateFileName());
                var appPoolStartMode = webStrategy.AppPoolStartMode == ApplicationPoolStartMode.None
                                           ? ApplicationPoolStartMode.OnDemand
                                           : webStrategy.AppPoolStartMode;

                var autoStartProviderName = webStrategy.AutoStartProvider.Name;
                var autoStartProviderType = webStrategy.AutoStartProvider.Type;

                var enableSni = false;
                var addHostHeaders = true;
                var installWebParameters = new object[]
                                               {
                                                   packageDirectoryPath, 
                                                   webStrategy.PrimaryDns, 
                                                   certificateTargetPath,
                                                   certDetails.CertificatePassword, 
                                                   appPoolStartMode,
                                                   autoStartProviderName,
                                                   autoStartProviderType,
                                                   enableSni, 
                                                   addHostHeaders,
                                               };

                ret.Add(
                    new SetupStep()
                        {
                            Description = "Send certificate file",
                            SetupAction =
                                (machineManager) => machineManager.SendFile(certificateTargetPath, certDetails.FileBytes)
                        });

                ret.Add(
                    new SetupStep()
                        {
                            Description = "Install IIS and tools",
                            SetupAction =
                                (machineManager) => machineManager.RunScript(InstallScriptBlocks.InstallWeb, installWebParameters)
                        });
            }
            else if (packagedConfig.GetType() == typeof(InitializationStrategyConsole))
            {
                throw new NotImplementedException();
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
    }
}
