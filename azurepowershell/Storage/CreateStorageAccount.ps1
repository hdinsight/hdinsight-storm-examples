#returns account primary key when successful otherwise $null
[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$AccountName,                         # required    needs to be alphanumeric or "-"
    [String]$Location="West Europe"               # optional    default to "West Europe"
    )

Write-Host "Creating Storage Account"
$Result = New-AzureStorageAccount -StorageAccountName $AccountName -Location "$Location"
Write-Host "$Result"

Write-Host "Getting Storage Key"
$PrimaryKey = $(Get-AzureStorageKey -StorageAccountName $AccountName).Primary

Write-Host "Successfully created Storage Account [$AccountName] in [$Location]"
return $PrimaryKey
