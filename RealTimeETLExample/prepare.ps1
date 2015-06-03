###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\scripts\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit /b -9999
}

$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################
$inputConfig = @{
EVENTHUBS="true"
HBASE="true"
EVENTHUBS_PARTITION_COUNT=16
}

#Create Run Configuration
& "$scriptDir\..\scripts\config\CreateRunConfig.ps1" $scriptDir "realtimeetl" $inputConfig
if(-not $?)
{
    Write-ErrorLog "Creation of Run Configuration failed. Please check the logs for error information or retry." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Creation of Run Configuration failed. Please check the logs for error information or retry."
}

#Create Azure Resources
& "$scriptDir\..\scripts\azure\CreateAzureResources.ps1" $scriptDir
if(-not $?)
{
    Write-ErrorLog "Creation of Azure Resources failed. Please check the logs for error information or retry." (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Creation of Azure Resources failed. Please check the logs for error information or retry."
}

#Update Project Properties
$configFile = Join-Path $scriptDir "run\configurations.properties"
$config = & "$scriptDir\..\scripts\config\ReadConfig.ps1" $configFile

$updateConfig = @{
EventHubFqnAddress=$config["EVENTHUBS_FQDN_SUFFIX"]
EventHubNamespace=$config["EVENTHUBS_NAMESPACE"]
EventHubEntityPath=$config["EVENTHUBS_ENTITY_PATH"]
EventHubUsername=$config["EVENTHUBS_USERNAME"]
EventHubPassword=$config["EVENTHUBS_PASSWORD"]
EventHubPartitions=$config["EVENTHUBS_PARTITION_COUNT"]
HBaseClusterUrl=$config["HBASE_CLUSTER_URL"]
HBaseClusterUserName=$config["HBASE_CLUSTER_USERNAME"]
HBaseClusterUserPassword=$config["HBASE_CLUSTER_PASSWORD"]
}

$topologyDir = Join-Path $scriptDir "EventHubAggregatorToHBaseTopology"

& "$scriptDir\..\scripts\scpnet\UpdateScpHostConfig.ps1" "$topologyDir\App.config" $updateConfig