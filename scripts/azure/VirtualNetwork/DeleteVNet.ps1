[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$VNetConfigFilePath,
    [Parameter(Mandatory = $true)]
    [String]$VNetName
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


Write-InfoLog "Deleting Azure VNet Config for $VNetName" (Get-ScriptName) (Get-ScriptLineNumber)

$VNetConfig = Get-AzureVNetConfig 
if($VNetConfig -ne $null)
{
    Write-SpecialLog "Found an existing Azure Virtual Network Configuration:" (Get-ScriptName) (Get-ScriptLineNumber)
    Write-InfoLog $VNetConfig.XMLConfiguration (Get-ScriptName) (Get-ScriptLineNumber)
    [xml] $VNetConfigXml = $VNetConfig.XMLConfiguration
    Write-InfoLog "Checking if it has $VNetName VNet" (Get-ScriptName) (Get-ScriptLineNumber)
    if(@($VNetConfigXml.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites | % {$_.VirtualNetworkSite} | ? { $_.name -eq $VNetName }).Count -gt 0)
    {
        Write-SpecialLog "$VNetName VNet was found, removing it from the VNet config" (Get-ScriptName) (Get-ScriptLineNumber)
        $VNetConfigXml.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites | % {$_.VirtualNetworkSite} | ? { $_.name -eq $VNetName } | % { $VNetConfigXml.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.RemoveChild($_) }
        $VNetConfigXml.Save($VNetConfigFilePath)
        Write-InfoLog ("Updated VNetConfig saved at: {0}" -f $VNetConfigFilePath) (Get-ScriptName) (Get-ScriptLineNumber)
        $VNetConfig = $null
    }
    else
    {
        Write-SpecialLog "$VNetName VNet was not found, no action required." (Get-ScriptName) (Get-ScriptLineNumber)
    }
}

if($VNetConfig -eq $null)
{
    try
    {
        Write-SpecialLog "Setting Azure VNet Configuration" (Get-ScriptName) (Get-ScriptLineNumber)
        $VNetConfig = Set-AzureVNetConfig -ConfigurationPath $VNetConfigFilePath
        Write-SpecialLog "Done. Getting Azure VNet Configuration" (Get-ScriptName) (Get-ScriptLineNumber)
        $VNetConfig = Get-AzureVNetConfig
    }
    catch
    {
        Write-ErrorLog ("Failed to set Azure VNet Configuration using file: $VNetConfigFilePath") (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw ("Failed to set Azure VNet Configuration using file: $VNetConfigFilePath")
    }
}

Write-InfoLog ("Azure VNet Config:`r`n" + $VNetConfig.XMLConfiguration) (Get-ScriptName) (Get-ScriptLineNumber)
return $VNetConfig