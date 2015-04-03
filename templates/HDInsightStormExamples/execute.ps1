###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\..\scripts\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

& "$scriptDir\build.ps1"

#Copy the "run\configurations.properties" from any other example here to run this project

$configFile = Join-Path $scriptDir "run\configurations.properties"
$config = & "$scriptDir\..\..\scripts\config\ReadConfig.ps1" $configFile
Select-AzureSubscription -SubscriptionName $config["AZURE_SUBSCRIPTION_NAME"]

$topologyDir = $scriptDir
$topologyBinDir = Join-Path $topologyDir "bin\Debug"

Remove-Item -Recurse "$topologyBinDir\ScpPackage" -ErrorAction SilentlyContinue

###########################################################
# HDInsightStormExamples
###########################################################
& "$scriptDir\..\..\scripts\scpnet\CreateScpSpec.ps1" "$topologyBinDir\HDInsightStormExamples.dll" "$topologyDir\HDInsightStormExamples.spec"
& "$scriptDir\..\..\scripts\scpnet\CreateScpPackage.ps1" "$topologyBinDir" "$topologyDir\HDInsightStormExamples.zip" "$scriptDir\..\..\lib\eventhubs"
& "$scriptDir\..\..\scripts\scpnet\SubmitSCPNetTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] "$topologyDir\HDInsightStormExamples.spec" "$topologyDir\HDInsightStormExamples.zip"

#Sleep a while for topologies to get started
sleep -s 15

& "$scriptDir\..\..\scripts\storm\GetStormSummary.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]
& "$scriptDir\..\..\scripts\storm\LaunchStormUI.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]