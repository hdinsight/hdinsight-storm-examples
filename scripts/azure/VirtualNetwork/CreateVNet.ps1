[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$VNetConfigFilePath,
    [Parameter(Mandatory = $true)]
    [String]$Location,
    [String]$VNetName,
    [String]$SubnetName
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

$VNetConfigTemplatePath = Join-Path $scriptDir "hdinsightstormexamples.netcfg"

Write-SpecialLog "Create Azure VNet using configuration file: $VNetConfigFilePath" (Get-ScriptName) (Get-ScriptLineNumber)

$VNetConfig = Get-AzureVNetConfig
if($VNetConfig -ne $null)
{
    Write-SpecialLog "Found an existing Azure Virtual Network Configuration:" (Get-ScriptName) (Get-ScriptLineNumber)
    Write-InfoLog $VNetConfig.XMLConfiguration (Get-ScriptName) (Get-ScriptLineNumber)
    [xml] $VNetConfigXml = $VNetConfig.XMLConfiguration
    Write-InfoLog "Checking if it has $VNetName VNet" (Get-ScriptName) (Get-ScriptLineNumber)
    if(@($VNetConfigXml.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites | % {$_.VirtualNetworkSite} | ? { $_.name -eq $VNetName }).Count -eq 0)
    {
        Write-SpecialLog "$VNetName VNet was not found, appending it to the VNet configuration" (Get-ScriptName) (Get-ScriptLineNumber)
        [xml] $vnetSiteNode = '
        <VirtualNetworkSite name="' + $VNetName + '" Location="' + $Location + '">
            <AddressSpace>
              <AddressPrefix>10.0.0.0/20</AddressPrefix>
            </AddressSpace>
            <Subnets>
              <Subnet name="' + $SubnetName + '">
                <AddressPrefix>10.0.0.0/20</AddressPrefix>
              </Subnet>
            </Subnets>
        </VirtualNetworkSite>';
        
        $vnetSiteImportXml = $VNetConfigXml.ImportNode($vnetSiteNode.DocumentElement, $true)
        $dummy = $VNetConfigXml.NetworkConfiguration.VirtualNetworkConfiguration.VirtualNetworkSites.AppendChild($vnetSiteImportXml)
        $VNetConfigXml = [xml] $VNetConfigXml.OuterXml.Replace(" xmlns=`"`"", "")
        $dummy = $VNetConfigXml.Save($VNetConfigFilePath)
        Write-InfoLog ("Updated VNetConfig saved at: {0}" -f $VNetConfigFilePath) (Get-ScriptName) (Get-ScriptLineNumber)
        $VNetConfig = $null
    }
    else
    {
        Write-SpecialLog "Found $VNetName VNet, no action required." (Get-ScriptName) (Get-ScriptLineNumber)
    }
}
else
{
    $dummy = Copy-Item -Force $VNetConfigTemplatePath $VNetConfigFilePath
    [xml] $VNetConfigXml = Get-Content $VNetConfigFilePath
    $VNetConfigXml = [xml] $VNetConfigXml.OuterXml.Replace("hdinsightstormexamples", $VNetName)
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

Write-InfoLog ("Azure VNet Configuration:`r`n" + $VNetConfig.XMLConfiguration) (Get-ScriptName) (Get-ScriptLineNumber)

$VNetSite = Get-AzureVNetSite $VNetName
Write-InfoLog ("Azure VNet Site:`r`n" + ($VNetSite | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
$vnetId = $VNetSite.Id
if($vnetId)
{
	Write-SpecialLog "Returning VNet Id: $vnetId for VNet: $VNetName" (Get-ScriptName) (Get-ScriptLineNumber)
	return $vnetId
}
else
{
	Write-ErrorLog "Unable to get Id for VNet: $VNetName" (Get-ScriptName) (Get-ScriptLineNumber)
	throw "Unable to get Id for VNet: $VNetName"
}