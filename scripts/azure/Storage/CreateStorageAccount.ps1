#returns account primary key when successful otherwise $null
[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$AccountName,                         # required    needs to be alphanumeric or "-"
    [String]$Location="West Europe"               # optional    default to "West Europe"
    )

try
{
    $Result = Get-AzureStorageAccount -StorageAccountName $AccountName
}
catch {}

if($Result -ne $null)
{
    Write-SpecialLog "Found an existing Storage account with same name: $AccountName" (Get-ScriptName) (Get-ScriptLineNumber)
}
else
{
    Write-InfoLog "Creating Storage Account: $AccountName in location: $Location" (Get-ScriptName) (Get-ScriptLineNumber)
    $Result = New-AzureStorageAccount -StorageAccountName $AccountName -Location "$Location"
    Write-InfoLog ($Result | Out-String) (Get-ScriptName) (Get-ScriptLineNumber)
}

Write-InfoLog "Getting Storage Key for $AccountName" (Get-ScriptName) (Get-ScriptLineNumber)
$PrimaryKey = $(Get-AzureStorageKey -StorageAccountName $AccountName).Primary

Write-SpecialLog "Successfully created Storage Account: $AccountName in location: $Location" (Get-ScriptName) (Get-ScriptLineNumber)
return $PrimaryKey