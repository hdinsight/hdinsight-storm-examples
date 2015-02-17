[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$Namespace,                             # required    needs to be alphanumeric or '-'
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$Path                                   # required    needs to be alphanumeric or '-'
    )

Write-Host "Adding the [Microsoft.ServiceBus.dll] assembly to the script..."
Add-Type -Path "$(Split-Path $script:MyInvocation.MyCommand.Path)\Microsoft.ServiceBus.dll"
Write-Host "The [Microsoft.ServiceBus.dll] assembly has been successfully added to the script."

$CurrentNamespace = Get-AzureSBNamespace -Name $Namespace

if ($CurrentNamespace)
{
    Write-Host "Connecting to namespace [$Namespace] in the [$($CurrentNamespace.Region)] region." 
    $NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($CurrentNamespace.ConnectionString);
    Write-Host "NamespaceManager object for the [$Namespace] namespace has been successfully created."
    Write-Host "Deleting EventHubs entity"
    $NamespaceManager.DeleteEventHub($Path)

    Write-Host "Deleting ServiceBus Namespace"
    Remove-AzureSBNamespace -Name $Namespace -Force

    Write-Host "Done deleting both EventHubs entity and namespace"
}
else
{
    Write-Host "The namespace [$Namespace] does not exists." 
}