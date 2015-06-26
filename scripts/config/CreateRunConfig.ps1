[CmdletBinding(PositionalBinding=$True)]
Param(
    [parameter(Mandatory=$true)]
    [string]$ExampleDir,
    [string]$ExamplePrefix,
    [hashtable]$InputConfig
    )

###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit -9999
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

###########################################################
# Create Run Configuration
###########################################################
$configFile = Join-Path $ExampleDir "run\configurations.properties"

Write-SpecialLog "Step 0: Creating Run Configuration" (Get-ScriptName) (Get-ScriptLineNumber)

& "$scriptDir\GenerateRandomConfig.ps1" $configFile $ExamplePrefix

$defaultConfig=@{
AZURE_LOCATION="West Europe"
STORM_CLUSTER_SIZE=4
HBASE_CLUSTER_SIZE=4
KAFKA_CLUSTER_SIZE=4
}

#Update any passed in input configurations. This is mostly used for examples to specify different components to deploy
if($InputConfig -ne $null)
{
    
    Write-InfoLog "Merging Configurations with passed in values" (Get-ScriptName) (Get-ScriptLineNumber)
    foreach($key in $InputConfig.keys) {
        $defaultConfig[$key] = $InputConfig[$key]
    }
}

Write-InfoLog "Updating Configurations with passed in values" (Get-ScriptName) (Get-ScriptLineNumber)
& "$scriptDir\ReplaceStringInFile.ps1" $configFile $configFile $defaultConfig

$config = & "$scriptDir\ReadConfig.ps1" $configFile
$config.Keys | sort | % { if(-not ($_.Contains("PASSWORD") -or $_.Contains("KEY"))) { Write-InfoLog ("Key = " + $_ + ", Value = " + $config[$_]) (Get-ScriptName) (Get-ScriptLineNumber) } }