[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$ResourceGroupName,                # required - needs to be alphanumeric or "-"
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

# Get the storage account key
$accountkey = $(Get-AzureRmStorageAccountKey -ResourceGroupName $ResourceGroupName -StorageAccountName $AccountName).Key1

# Create the storage context object
$destContext = New-AzureStorageContext -StorageAccountName $AccountName -StorageAccountKey $accountkey

# Check if the container exists
try
{
    Write-InfoLog "Checking if the container $ContainerName exists in Storage account: [$AccountName]" (Get-ScriptName) (Get-ScriptLineNumber)
    $container = Get-AzureStorageContainer -Name $ContainerName -context $destContext
}
catch
{
    if($container -eq $null)
    {
        Write-InfoLog "Creating the container $ContainerName with permission [Blob] in Storage account: [$AccountName]" (Get-ScriptName) (Get-ScriptLineNumber)
        $container = New-AzureStorageContainer -Name $ContainerName -Permission Blob -context $destContext
    }
    else
    {
        throw
    }
}

Write-InfoLog "Copying the file [$FilePath] from local to Blob [$BlobName]" (Get-ScriptName) (Get-ScriptLineNumber)
$blobUpload = Set-AzureStorageBlobContent -File $FilePath -Container $ContainerName -Blob $BlobName -context $destContext -Force
Write-InfoLog ("[SUCCESS] Uploaded blob to: {0}" -f $blobUpload.ICloudBlob.StorageUri.PrimaryUri.AbsoluteUri) (Get-ScriptName) (Get-ScriptLineNumber)
return $blobUpload.ICloudBlob.StorageUri.PrimaryUri.AbsoluteUri