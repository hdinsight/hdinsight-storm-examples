[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [String]$ClusterUrl,
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword,
    [Parameter(Mandatory = $true)]
    [string]$SpecFile,
    [Parameter(Mandatory = $true)]
    [string]$PackageFile
)

###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit -9999
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

$scpTools = & "$scriptDir\GetScpTools.ps1"
$ScpWebApiClient = Join-Path $scpTools "ScpWebApiClient.exe"

$clusterUri = New-Object Uri($ClusterUrl)
$scpApiUrl = New-Object Uri($clusterUri, "scpapi")

Write-InfoLog "Cluster ScpApiUrl: $scpApiUrl, User: $ClusterUsername - TopologySpec: $SpecFile, TopologyPackage: $PackageFile, TopologyAction: submit" (Get-ScriptName) (Get-ScriptLineNumber)
& "$ScpWebApiClient" submit -scpApiUrl $scpApiUrl -username $ClusterUsername -password $ClusterPassword -spec $SpecFile -packageFile $PackageFile -timeout 900

if($LASTEXITCODE -ne 0)
{
    throw "ERROR: Failed to submit topology. Please check error logs for more information."
}