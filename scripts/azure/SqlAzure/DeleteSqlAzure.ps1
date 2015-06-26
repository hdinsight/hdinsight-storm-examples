[CmdletBinding(PositionalBinding=$true)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ServerName                     # required    needs to be alphanumeric
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

Write-InfoLog "Deleting SQL Server [$ServerName]" (Get-ScriptName) (Get-ScriptLineNumber)
Remove-AzureSqlDatabaseServer $ServerName -Force

Write-InfoLog "Successfully deleted server [$ServerName]" (Get-ScriptName) (Get-ScriptLineNumber)