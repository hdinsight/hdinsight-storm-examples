[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$AccountName                     # required    needs to be alphanumeric or "-"
    )

#Switch to Azure resource manager mode
Switch-AzureMode -Name AzureResourceManager

$ResourceGroupName="{0}{1}" -f $AccountName,"Group"
#Remove-AzureResource -ApiVersion 2014-07-10 -Name shanyutestauto -ResourceGroupName ShanyuResourceGroup -ResourceType "Microsoft.DocumentDb/databaseAccounts" -Force

Write-Host "Deleting DocumentDB resources, it may take a while..."
Remove-AzureResourceGroup -Name $ResourceGroupName -Force

Switch-AzureMode -Name AzureServiceManagement