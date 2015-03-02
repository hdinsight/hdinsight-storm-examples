param($installPath, $toolsPath, $package, $project)
$scpHostConfig = $project.ProjectItems.Item("SCPHost.exe.config")
 
# set 'Copy To Output Directory' to 'Copy always'
$scpHostConfigCopyAttribute = $scpHostConfig.Properties.Item("CopyToOutputDirectory")
$scpHostConfigCopyAttribute.Value = 1
