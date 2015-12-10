[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [ValidateLength(8,64)]
    [String]$ResourceGroupName,              # required - needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$AccountName                     # required - needs to be alphanumeric or "-"
    )

###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit -9999
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

$ResourceType="Microsoft.DocumentDb/databaseAccounts"
$ApiVersion="2015-04-08"

Write-InfoLog "Deleting DocumentDB resources, it may take a while..." (Get-ScriptName) (Get-ScriptLineNumber)

try
{
    Remove-AzureRmResource -ResourceGroupName $ResourceGroupName -Name $AccountName -ResourceType $ResourceType -ApiVersion $ApiVersion -Force
}
catch
{
    Write-ErrorLog "Failed to delete DocumentDB" (Get-ScriptName) (Get-ScriptLineNumber) $_
    throw
}
