#returns account primary key when successful otherwise $null
[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$ResourceGroupName,                # required - needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [String]$Location,                         # required
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

$startTime = Get-Date

& "$scriptDir\..\CreateAzureResourceGroup.ps1" $ResourceGroupName $Location

try
{
    $Result = Get-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName -StorageAccountName $AccountName
}
catch {}

if($Result -ne $null)
{
    Write-InfoLog "Found an existing Storage account with same name: $AccountName" (Get-ScriptName) (Get-ScriptLineNumber)
}
else
{
    Write-InfoLog "Creating Storage Account: $AccountName in Resource Group: $ResourceGroupName at location: $Location" (Get-ScriptName) (Get-ScriptLineNumber)
    $Result = New-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName -StorageAccountName $AccountName -Location "$Location" -Type "Standard_LRS"
    Write-InfoLog ($Result | Out-String) (Get-ScriptName) (Get-ScriptLineNumber)
}

Write-InfoLog "Getting Storage Key for $AccountName" (Get-ScriptName) (Get-ScriptLineNumber)
$PrimaryKey = $(Get-AzureRmStorageAccountKey -ResourceGroupName $ResourceGroupName -StorageAccountName $AccountName).Key1

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-SpecialLog "Storage Account: $AccountName successfully created in Resource Group: $ResourceGroupName at Location: $Location. Time: $totalSeconds secs" (Get-ScriptName) (Get-ScriptLineNumber)

return $PrimaryKey