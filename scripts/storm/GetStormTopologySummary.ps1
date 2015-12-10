[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ClusterUrl,
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword,
    [Parameter(Mandatory = $true)]
    [String]$TopologyName
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
$topologySummaryUri = new-object Uri($clusterUri, "stormui/api/v1/topology/summary")

$response = Invoke-RestMethod -Uri $topologySummaryUri.AbsoluteUri -Method Get -Credential $clusterCreds
$response = $response.topologies | Out-String
Write-InfoLog ("Topology Summary: " + $topologySummaryUri.AbsoluteUri + "`r`n" + $response) (Get-ScriptName) (Get-ScriptLineNumber)

$topology = $response.topologies | ? { $_.Name -eq $TopologyName }
$topologyUri = new-object Uri($clusterUri, "stormui/api/v1/topology/" + $topology.Id)

$response = Invoke-RestMethod -Uri $topologyUri.AbsoluteUri -Method Get -Credential $clusterCreds
Write-InfoLog ("$TopologyName Topology Summary: " + $topologyUri.AbsoluteUri + "`r`n" + $response) (Get-ScriptName) (Get-ScriptLineNumber)
