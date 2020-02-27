param($installPath, $toolsPath, $package, $project)
#$configItem = $project.ProjectItems.Item("NLog.config")
# set 'Copy To Output Directory' to ?'0:Never, 1:Always, 2:IfNewer'
#$configItem.Properties.Item("CopyToOutputDirectory").Value = 2
# set 'Build Action' to ?'0:None, 1:Compile, 2:Content, 3:EmbeddedResource'
#$configItem.Properties.Item("BuildAction").Value = 2

# .config\Common\ComputingInfrastructureManagerSettings.json
$configItem8bd82e6eb914408d9a371246295dd0ed = $project.ProjectItems.Item(".config").ProjectItems.Item("Common").ProjectItems.Item("ComputingInfrastructureManagerSettings.json")
$configItem8bd82e6eb914408d9a371246295dd0ed.Properties.Item("CopyToOutputDirectory").Value = 1
$configItem8bd82e6eb914408d9a371246295dd0ed.Properties.Item("BuildAction").Value = 2

# .config\Common\DefaultDeploymentConfiguration.json
$configItemcba858a4c5974d1487c52926e7b71be7 = $project.ProjectItems.Item(".config").ProjectItems.Item("Common").ProjectItems.Item("DefaultDeploymentConfiguration.json")
$configItemcba858a4c5974d1487c52926e7b71be7.Properties.Item("CopyToOutputDirectory").Value = 1
$configItemcba858a4c5974d1487c52926e7b71be7.Properties.Item("BuildAction").Value = 2

# .config\Common\SetupStepFactorySettings.json
$configItem6ec3fe3dfd424acf8dd29414b7966b96 = $project.ProjectItems.Item(".config").ProjectItems.Item("Common").ProjectItems.Item("SetupStepFactorySettings.json")
$configItem6ec3fe3dfd424acf8dd29414b7966b96.Properties.Item("CopyToOutputDirectory").Value = 1
$configItem6ec3fe3dfd424acf8dd29414b7966b96.Properties.Item("BuildAction").Value = 2
