[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9]*$")]
    [String]$accountName,
    [Parameter(Mandatory = $true)]
    [String]$containerName,
    [Parameter(Mandatory = $true)]
    [String]$fileName,
    [Parameter(Mandatory = $true)]
    [String]$blobName
    )


# Get the storage account key
$accountkey = get-azurestoragekey $accountName | %{$_.Primary}

# Create the storage context object
$destContext = New-AzureStorageContext -StorageAccountName $accountName -StorageAccountKey $accountkey

Write-Host "Copying the file [$fileName] from local workstation to Blob [$blobName]"
Set-AzureStorageBlobContent -File $fileName -Container $containerName -Blob $blobName -context $destContext -Force

Write-Host "Successfully copied file"