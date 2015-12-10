[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-zA-Z0-9-]*$")]
    [ValidateLength(8,64)]
    [String]$ResourceGroupName              # required - needs to be alphanumeric or "-"
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

$startTime = Get-Date

try
{
    Write-InfoLog "Deleting Azure Resource Group: $ResourceGroupName and all resources in it, it may take a while..." (Get-ScriptName) (Get-ScriptLineNumber)
    Remove-AzureRmResourceGroup -Name $ResourceGroupName -Force
}
catch
{
    Write-ErrorLog "Failed to delete Azure Resource Group: $ResourceGroupName" (Get-ScriptName) (Get-ScriptLineNumber) $_
    throw
}

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-InfoLog "Resource Group: $ResourceGroupName deleted successfully. Time: $totalSeconds secs" (Get-ScriptName) (Get-ScriptLineNumber)
