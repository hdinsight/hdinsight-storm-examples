[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$AccountName                     # required    needs to be alphanumeric or "-"
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

$ResourceGroupName="{0}{1}" -f $AccountName,"Group"
#Remove-AzureResource -ApiVersion 2014-07-10 -Name shanyutestauto -ResourceGroupName ShanyuResourceGroup -ResourceType "Microsoft.DocumentDb/databaseAccounts" -Force

Write-InfoLog "Deleting DocumentDB resources, it may take a while..." (Get-ScriptName) (Get-ScriptLineNumber)
try
{
    #Switch to Azure resource manager mode
    Switch-AzureMode -Name AzureResourceManager
    Remove-AzureResourceGroup -Name $ResourceGroupName -Force
}
catch
{
    Write-ErrorLog "Failed to delete DocumentDB" (Get-ScriptName) (Get-ScriptLineNumber) $_
    throw
}
finally
{
    Switch-AzureMode -Name AzureServiceManagement
}