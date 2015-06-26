[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigFile,
    [Parameter(Mandatory = $true)]
    [hashtable]$updateConfig
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

$appConfig = New-Object XML
$appConfig.Load($ConfigFile)

foreach($appSetting in $appConfig.configuration.appSettings.add)
{
    if($updateConfig.ContainsKey($appSetting.key))
    {
        Write-InfoLog ("Updating " + $appSetting.key + " with new value") (Get-ScriptName) (Get-ScriptLineNumber)
        $appSetting.value = $updateConfig[$appSetting.key]
    }
}

$appConfig.Save($ConfigFile)