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

$inputConfig = @{
EVENTHUBS="true"
SQLAZURE="true"
EVENTHUBS_PARTITION_COUNT=32
STORM_CLUSTER_SIZE=32
}

#Create Run Configuration
& "$scriptDir\..\scripts\config\CreateRunConfig.ps1" $scriptDir "eventcount" $inputConfig
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

& "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" "$scriptDir\EventGenTopology\myconfig.properties.template" "$scriptDir\EventGenTopology\src\main\resources\myconfig.properties" $config
& "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" "$scriptDir\EventCountTopology\myconfig.properties.template" "$scriptDir\EventCountTopology\src\main\resources\myconfig.properties" $config