#returns SQL Database server name
[CmdletBinding(PositionalBinding=$true)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$ResourceGroupName,              # required - needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [String]$ServerName                     # required - OKAY to pass empty string to create one
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

try
{
    Write-InfoLog "Deleting Sql Server: $ServerName" (Get-ScriptName) (Get-ScriptLineNumber)
    Remove-AzureRmSqlServer -ResourceGroupName $ResourceGroupName -ServerName $ServerName -Force
    Write-InfoLog "Successfully deleted Sql Server: $ServerName" (Get-ScriptName) (Get-ScriptLineNumber)
}
catch
{
    if ($_.Exception.Error.Code -eq "ResourceNotFound")
    {
        Write-InfoLog "Success! Sql server not found: $ServerName" (Get-ScriptName) (Get-ScriptLineNumber) $_
    }
    elseif ($_.Exception.Error.Code -eq "ParentResourceNotFound")
    {
        Write-WarnLog "An unexpected error occured while deleting the Sql Server: $ServerName" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
    else
    {
        Write-ErrorLog "Could not get details for Sql Server: $ServerName" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}
