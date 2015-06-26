[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$VirtualNetworkName,                # required    needs to be alphanumeric or "-"
    [String]$Location = "West Europe",          # optional
    [String]$AddressPrefix = "10.0.0.0/20"      # optional
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

#Switch to Azure resource manager mode
$result = Switch-AzureMode -Name AzureResourceManager

$ResourceGroupName="{0}{1}" -f $VirtualNetworkName,"Group"
$ResourceType="Microsoft.Network/virtualNetworks"
$ApiVersion="2015-06-15"

$startTime = Get-Date

$resourceGroupStatus = $false
$resourceStatus = $false

try
{
    Write-InfoLog "Trying to find AzureResourceGroup: $ResourceGroupName" (Get-ScriptName) (Get-ScriptLineNumber)
    $ResourceGroup = Get-AzureResourceGroup -Name $ResourceGroupName
    $resourceGroupStatus = $true
}
catch 
{
    Write-WarnLog "Could not find ResourceGroup account: $ResourceGroupName. Will attempt to create a new one." (Get-ScriptName) (Get-ScriptLineNumber)
}

if($ResourceGroup -eq $null)
{
    try
    {
        $ResourceGroup = New-AzureResourceGroup -Name $ResourceGroupName -Location "$Location" -Force
    }
    catch
    {
        Write-ErrorLog "Failed to create AzureResourceGroup." (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}

Write-InfoLog ("AzureResourceGroup Information:`r`n" + ($ResourceGroup | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
        
try
{
    Write-InfoLog "Trying to find Virtual Network: $VirtualNetworkName in ResourceGroup: $ResourceGroupName" (Get-ScriptName) (Get-ScriptLineNumber)
    $VNet = Get-AzureVirtualNetwork -Name $VirtualNetworkName -ResourceGroupName $ResourceGroupName
}
catch 
{
    Write-WarnLog "Could not find Virtual Network: $VirtualNetworkName. Will attempt to create a new one." (Get-ScriptName) (Get-ScriptLineNumber)
}

if($VNet -eq $null)
{
    Write-InfoLog "Creating Virtual Network: $VirtualNetworkName" (Get-ScriptName) (Get-ScriptLineNumber)
    try
    {
        $subnetName = "Subnet-1"
        $subnet = New-AzureVirtualNetworkSubnetConfig -Name $subnetName -AddressPrefix $AddressPrefix
        $VNet = New-AzureVirtualNetwork -Name $VirtualNetworkName -ResourceGroupName $ResourceGroupName -Location $Location -AddressPrefix $AddressPrefix -Subnet $subnet
    }
    catch
    {
        Write-ErrorLog "Failed to create Virtual Network: $VirtualNetworkName" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}

if($VNet)
{
    $vnetStatus = $true
    Write-InfoLog ("Azure Virtual Network Information:`r`n" + ($VNet | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
    $subnet = Get-AzureVirtualNetworkSubnetConfig -Name $subnetName -VirtualNetwork $VNet
    Write-InfoLog ("Subnet Information:`r`n" + ($subnet | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
}

$result = Switch-AzureMode -Name AzureServiceManagement

if(-not $vnetStatus)
{
    Write-ErrorLog "Failed to create Azure Virtual Network" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Failed to create Azure Virtual Network"
}