[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$Namespace,                             # required    needs to be alphanumeric or '-'
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$Path                                   # required    needs to be alphanumeric or '-'
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

$serviceBusDll = & "$scriptDir\GetServiceBusDll.ps1"

Write-InfoLog "Adding the assembly: '$serviceBusDll' to the script." (Get-ScriptName) (Get-ScriptLineNumber)
Add-Type -Path $serviceBusDll
Write-InfoLog "The assembly: '$serviceBusDll' has been successfully added to the script." (Get-ScriptName) (Get-ScriptLineNumber)

try
{
    $CurrentNamespace = Get-AzureSBNamespace -Name $Namespace
}
catch
{
    Write-WarnLog "Azure Service Bus Namespace: $Namespace not found!" (Get-ScriptName) (Get-ScriptLineNumber)
}

if ($CurrentNamespace)
{
    #Write-InfoLog "Connecting to namespace [$Namespace] in the [$($CurrentNamespace.Region)] region." (Get-ScriptName) (Get-ScriptLineNumber)
    #$NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($CurrentNamespace.ConnectionString);
    #Write-InfoLog "NamespaceManager object for the [$Namespace] namespace has been successfully created." (Get-ScriptName) (Get-ScriptLineNumber)
    #Write-InfoLog "Deleting EventHubs entity" (Get-ScriptName) (Get-ScriptLineNumber)
    #$NamespaceManager.DeleteEventHub($Path)

    Write-InfoLog "Deleting ServiceBus Namespace" (Get-ScriptName) (Get-ScriptLineNumber)
    try
    {
        Remove-AzureSBNamespace -Name $Namespace -Force
        Write-InfoLog "Delete Azure Service Bus Namespace: $Namespace" (Get-ScriptName) (Get-ScriptLineNumber)
    }
    catch
    {
        Write-ErrorLog "Failed to delete Azure Service Bus Namespace: $Namespace" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}
else
{
    Write-InfoLog "The namespace: $Namespace does not exists."  (Get-ScriptName) (Get-ScriptLineNumber)
}