###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\scripts\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

$configFile = Join-Path $scriptDir "run\configurations.properties"
$config = & "$scriptDir\..\scripts\config\ReadConfig.ps1" $configFile
Select-AzureSubscription -SubscriptionName $config["AZURE_SUBSCRIPTION_NAME"]

$topologyDir = Join-Path $scriptDir "EventHubAggregatorToHBaseTopology"
$topologyBinDir = Join-Path $topologyDir "bin\Debug"

Remove-Item -Recurse "$topologyBinDir\ScpPackage" -ErrorAction SilentlyContinue

###########################################################
# EventHubSenderHybridTopology
###########################################################
& "$scriptDir\..\scripts\scpnet\CreateScpSpec.ps1" "$topologyBinDir\EventHubAggregatorToHBaseTopology.dll" "$topologyDir\EventHubSenderHybridTopology.spec" "EventHubAggregatorToHBaseTopology.EventHubSenderHybridTopology"
& "$scriptDir\..\scripts\scpnet\CreateScpPackage.ps1" "$topologyBinDir" "$topologyDir\EventHubSenderHybridTopology.zip" "$scriptDir\..\lib\eventhubs"
& "$scriptDir\..\scripts\scpnet\SubmitSCPNetTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] "$topologyDir\EventHubSenderHybridTopology.spec" "$topologyDir\EventHubSenderHybridTopology.zip"

###########################################################
# EventHubAggregatorToHBaseHybridTopology
###########################################################
& "$scriptDir\..\scripts\scpnet\CreateScpSpec.ps1" "$topologyBinDir\EventHubAggregatorToHBaseTopology.dll" "$topologyDir\EventHubAggregatorToHBaseHybridTopology.spec" "EventHubAggregatorToHBaseTopology.EventHubAggregatorToHBaseHybridTopology"
& "$scriptDir\..\scripts\scpnet\CreateScpPackage.ps1" "$topologyBinDir" "$topologyDir\EventHubAggregatorToHBaseHybridTopology.zip" "$scriptDir\..\lib\eventhubs"
& "$scriptDir\..\scripts\scpnet\SubmitSCPNetTopology.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"] "$topologyDir\EventHubAggregatorToHBaseHybridTopology.spec" "$topologyDir\EventHubAggregatorToHBaseHybridTopology.zip"

#Sleep a while for topologies to get started
sleep -s 15

& "$scriptDir\..\scripts\storm\GetStormSummary.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]
& "$scriptDir\..\scripts\storm\LaunchStormUI.ps1" $config["STORM_CLUSTER_URL"] $config["STORM_CLUSTER_USERNAME"] $config["STORM_CLUSTER_PASSWORD"]