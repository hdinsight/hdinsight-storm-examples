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
DOCUMENTDB="true"
EVENTHUBS_PARTITION_COUNT=16
}

#Create Run Configuration
& "$scriptDir\..\scripts\config\CreateRunConfig.ps1" $scriptDir "iot" $inputConfig
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

& "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" "$scriptDir\docdbgen\docdb.config.template" "$scriptDir\run\docdb.config" $config
& "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" "$scriptDir\eventgen\eventhubs.config.template" "$scriptDir\run\eventhubs.config" $config
& "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" "$scriptDir\iot\myconfig.properties.template" "$scriptDir\iot\src\main\resources\myconfig.properties" $config
& "$scriptDir\..\scripts\config\ReplaceStringInFile.ps1" "$scriptDir\iot\core-site.xml.template" "$scriptDir\iot\src\main\resources\core-site.xml" $config