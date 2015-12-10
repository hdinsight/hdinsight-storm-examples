#returns account primary key when successful otherwise $null
[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$ResourceGroupName,                # required - needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$AccountName                       # required - needs to be alphanumeric or "-"
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
    Write-InfoLog "Deleting Storage Account: $AccountName" (Get-ScriptName) (Get-ScriptLineNumber)
    Remove-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName -StorageAccountName $AccountName
    Write-InfoLog "Successfully deleted Storage Account: $AccountName" (Get-ScriptName) (Get-ScriptLineNumber)
}
catch
{
    if ($_.Exception.Error.Code -eq "ResourceNotFound")
    {
        Write-InfoLog "Success! Storage Account not found: $AccountName" (Get-ScriptName) (Get-ScriptLineNumber) $_
    }
    elseif ($_.Exception.Error.Code -eq "ParentResourceNotFound")
    {
        Write-WarnLog "An unexpected error occured while deleting the Storage Account: $AccountName" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
    else
    {
        Write-ErrorLog "Could not get details for Storage Account: $AccountName" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}
