[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$ResourceGroupName,                # required - needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [String]$Location,                         # required
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$VirtualNetworkName,                # required - needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9-]*$")]
    [String]$SubnetName,                        # required - needs to be alphanumeric or "-"
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

$startTime = Get-Date

& "$scriptDir\..\CreateAzureResourceGroup.ps1" $ResourceGroupName $Location

try
{
    Write-InfoLog "Trying to find Virtual Network: $VirtualNetworkName in Azure Resource Group: $ResourceGroupName" (Get-ScriptName) (Get-ScriptLineNumber)
    $VirtualNetwork = Get-AzureRmVirtualNetwork -Name $VirtualNetworkName -ResourceGroupName $ResourceGroupName
}
catch 
{
    Write-WarnLog "Could not find Virtual Network: $VirtualNetworkName. Will attempt to create a new one." (Get-ScriptName) (Get-ScriptLineNumber)
}

if($VirtualNetwork -eq $null)
{
    Write-InfoLog "Creating Virtual Network: $VirtualNetworkName" (Get-ScriptName) (Get-ScriptLineNumber)
    try
    {
        $subnet = New-AzureRmVirtualNetworkSubnetConfig -Name $SubnetName -AddressPrefix $AddressPrefix
        $createVirtualNetwork = New-AzureRmVirtualNetwork -ResourceGroupName $ResourceGroupName -Name $VirtualNetworkName -Location $Location -AddressPrefix $AddressPrefix -Subnet $subnet
        $VirtualNetwork = Get-AzureRmVirtualNetwork -ResourceGroupName $ResourceGroupName -Name $VirtualNetworkName
    }
    catch
    {
        Write-ErrorLog "Failed to create Virtual Network: $VirtualNetworkName" (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw
    }
}

if($VirtualNetwork)
{
    $vnetStatus = $true
    Write-InfoLog ("Azure Virtual Network Information:`r`n" + ($VNet | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
    $subnet = Get-AzureRmVirtualNetworkSubnetConfig -Name $SubnetName -VirtualNetwork $VirtualNetwork
    Write-InfoLog ("Subnet Information:`r`n" + ($subnet | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
}
else
{
    Write-ErrorLog "Failed to create Azure Virtual Network" (Get-ScriptName) (Get-ScriptLineNumber)
    throw "Failed to create Azure Virtual Network"
}

$finishTime = Get-Date
$totalSeconds = ($finishTime - $startTime).TotalSeconds
Write-SpecialLog "Virtual Network: $VirtualNetworkName successfully created in Resource Group: $ResourceGroupName at Location: $Location. Time: $totalSeconds secs" (Get-ScriptName) (Get-ScriptLineNumber)

return $VirtualNetwork.Id