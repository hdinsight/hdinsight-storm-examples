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
$clusterSummaryUri = new-object Uri($clusterUri, "/api/v1/clusters/clusterName/hosts")

$response = Invoke-RestMethod -Uri $clusterSummaryUri.AbsoluteUri -Method Get -Credential $clusterCreds
$response = $response.hosts | Out-String
Write-InfoLog ("Cluster Summary: " + $topologySummaryUri.AbsoluteUri + "`r`n" + $response) (Get-ScriptName) (Get-ScriptLineNumber)

return $response