[CmdletBinding(PositionalBinding=$True)]
Param(
    [parameter(Mandatory=$true)]
    [string]$ExampleDir,
    [string]$ExamplePrefix,
    [hashtable]$AzureComponentList
    )

###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\init.ps1"
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

###########################################################
# Create Run Configuration
###########################################################
$configFile = Join-Path $ExampleDir "run\configurations.properties"

Write-SpecialLog "Step 0: Creating Run Configuration" (Get-ScriptName) (Get-ScriptLineNumber)

& "$scriptDir\GenerateRandomConfig.ps1" $configFile $ExamplePrefix

#Update any passed in input configurations. This is mostly used for examples to specify different components to deploy
if($inputConfig -ne $null)
{
    Write-InfoLog "Updating Configurations with passed in values" (Get-ScriptName) (Get-ScriptLineNumber)
    & "$scriptDir\ReplaceStringInFile.ps1" $configFile $configFile $inputConfig
}

$config = & "$scriptDir\ReadConfig.ps1" $configFile
$config.Keys | sort | % { if(-not ($_.Contains("PASSWORD") -or $_.Contains("KEY"))) { Write-SpecialLog ("Key = " + $_ + ", Value = " + $config[$_]) (Get-ScriptName) (Get-ScriptLineNumber) } }