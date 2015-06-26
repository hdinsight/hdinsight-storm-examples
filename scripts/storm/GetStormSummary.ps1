[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ClusterUrl,
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword
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

$secureClusterPassword = ConvertTo-SecureString $ClusterPassword -AsPlainText -Force
$clusterCreds = New-Object System.Management.Automation.PSCredential ($ClusterUsername, $secureClusterPassword)

$clusterUri = new-object Uri($ClusterUrl)
$clusterSummaryUri = new-object Uri($clusterUri, "stormui/api/v1/cluster/summary")
$topologySummaryUri = new-object Uri($clusterUri, "stormui/api/v1/topology/summary")

$response = Invoke-RestMethod -Uri $clusterSummaryUri.AbsoluteUri -Method Get -Credential $clusterCreds | Out-String
Write-InfoLog ("Cluster Summary: " + $clusterSummaryUri.AbsoluteUri + "`r`n" + $response) (Get-ScriptName) (Get-ScriptLineNumber)

$response = Invoke-RestMethod -Uri $topologySummaryUri.AbsoluteUri -Method Get -Credential $clusterCreds
$response = $response.topologies | Out-String
Write-InfoLog ("Topology Summary: " + $topologySummaryUri.AbsoluteUri + "`r`n" + $response) (Get-ScriptName) (Get-ScriptLineNumber)