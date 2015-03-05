[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$AccountName                    # required    needs to be alphanumeric or "-"
    )

Remove-AzureStorageAccount -StorageAccountName $AccountName
