[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9]*$")]
    [String]$AccountName,
    [Parameter(Mandatory = $true)]
    [String]$ContainerName,
    [Parameter(Mandatory = $true)]
    [String]$FilePath,
    [Parameter(Mandatory = $true)]
    [String]$BlobName
    )

Switch-AzureMode -Name AzureServiceManagement

# Get the storage account key
$accountkey = Get-AzureStorageKey $AccountName | %{$_.Primary}

# Create the storage context object
$destContext = New-AzureStorageContext -StorageAccountName $AccountName -StorageAccountKey $accountkey

Write-InfoLog "Copying the file [$FilePath] from local workstation to Blob [$BlobName]" (Get-ScriptName) (Get-ScriptLineNumber)
$blobUpload = Set-AzureStorageBlobContent -File $FilePath -Container $ContainerName -Blob $BlobName -context $destContext -Force
