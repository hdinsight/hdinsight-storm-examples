[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ClusterUrl,                           # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,                      # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword,                      # required
    [Parameter(Mandatory = $true)]
    [String]$TopologyName,                         # required
    [Parameter(Mandatory = $true)]
    [String]$TopologyAction,                       # required
    [String]$TopologyActionWait                    # optional
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
#Reference: https://github.com/apache/storm/blob/master/STORM-UI-REST-API.md

$clusterUri = new-object Uri($ClusterUrl)
$securePwd = ConvertTo-SecureString $ClusterPassword -AsPlainText -Force
$creds = New-Object System.Management.Automation.PSCredential($ClusterUsername, $securePwd)

#Get topologies
$topologySummaryUri = new-object Uri($clusterUri, "stormui/api/v1/topology/summary")
Write-InfoLog ("Sending GET request at: " + $topologySummaryUri) (Get-ScriptName) (Get-ScriptLineNumber)
try
{
    $response = Invoke-RestMethod -Uri $topologySummaryUri -Method Get -Credential $creds
    Write-InfoLog ("Response:`r`n" + ($response | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)

    $topologyId = ($response.topologies | ? { $_.name -like $TopologyName } | Select-Object -Last 1).id

    if([String]::IsNullOrWhitespace($topologyId))
    {
        Write-ErrorLog "No topologyId found in response for $TopologyName." (Get-ScriptName) (Get-ScriptLineNumber)
        throw "No topologyId found in response for $TopologyName."
    }
    else
    {
        Write-SpecialLog "Topology: $TopologyName - topologyId = $topologyId" (Get-ScriptName) (Get-ScriptLineNumber)
    }
}
catch
{
    Write-ErrorLog "Exception encountered while invoking the [GET] rest method at: $topologySummaryUri" (Get-ScriptName) (Get-ScriptLineNumber) $_
    throw "Exception encountered while invoking the [GET] rest method at: $topologySummaryUri"
}

#Get anti forgery token
$topologyUri = new-object Uri($clusterUri, ("stormui/api/v1/topology/{0}" -f $topologyId))
Write-InfoLog ("Sending GET request at: " + $topologyUri) (Get-ScriptName) (Get-ScriptLineNumber)
try
{
    $response = Invoke-RestMethod -Uri $topologyUri -Method Get -Credential $creds -SessionVariable webSession
    Write-InfoLog ("Response:`r`n" + ($response | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
    $antiForgeryToken=$response.antiForgeryToken
    if([String]::IsNullOrWhitespace($antiForgeryToken))
    {
        Write-ErrorLog "No antiForgeryToken found in response." (Get-ScriptName) (Get-ScriptLineNumber)
        throw "No antiForgeryToken found in response."
    }
    else
    {
        Write-SpecialLog "Topology: $TopologyName - antiForgeryToken = $antiForgeryToken" (Get-ScriptName) (Get-ScriptLineNumber)
    }
}
catch
{
    Write-ErrorLog "Exception encountered while invoking the [GET] rest method at: $topologyUri" (Get-ScriptName) (Get-ScriptLineNumber) $_
    throw "Exception encountered while invoking the [GET] rest method at: $topologyUri"
}

if($TopologyAction -like "get")
{
    Write-SpecialLog "$TopologyName - Spouts" (Get-ScriptName) (Get-ScriptLineNumber)
    Write-InfoLog ("Spouts" + ($response.spouts | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
    Write-SpecialLog "$TopologyName - Bolts" (Get-ScriptName) (Get-ScriptLineNumber)
    Write-InfoLog ("Bolts" + ($response.bolts | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
}
else
{
    if(($TopologyAction -like "kill") -or ($TopologyAction -like "rebalance"))
    {
        $TopologyActionWait = "0"
    }
    #Topology Action
    if([String]::IsNullOrWhitespace($TopologyActionWait))
    {
        $topologyManagementUri = new-object Uri($clusterUri, ("stormui/api/v1/topology/{0}/{1}" -f $topologyId,$TopologyAction))
    }
    else
    {
        $topologyManagementUri = new-object Uri($clusterUri, ("stormui/api/v1/topology/{0}/{1}/{2}" -f $topologyId,$TopologyAction,$TopologyActionWait))
    }

    Write-InfoLog ("Sending POST request at: " + $topologyManagementUri) (Get-ScriptName) (Get-ScriptLineNumber)
    try
    {
        $response = Invoke-RestMethod -Uri $topologyManagementUri -Method Post -Headers @{"x-csrf-token"=$antiForgeryToken} -Credential $creds -WebSession $webSession -MaximumRedirection 1
        Write-SpecialLog "Topology action '$TopologyAction' complete: $ClassName" (Get-ScriptName) (Get-ScriptLineNumber)
        Write-InfoLog ("Response:`r`n" + ($response | Out-String)) (Get-ScriptName) (Get-ScriptLineNumber)
    }
    catch
    {
        Write-ErrorLog "Exception encountered while invoking the [POST] rest method at: $topologyManagementUri" (Get-ScriptName) (Get-ScriptLineNumber) $_
        if($_.Exception.Response.StatusCode -eq [System.Net.HttpStatusCode]::Unauthorized)
        {
            Write-SpecialLog "Known Issue: Received a 401 status code in response. Ignoring the exception." (Get-ScriptName) (Get-ScriptLineNumber) $_
        }
        else
        {
            throw "Exception encountered while invoking the [POST] rest method at: $topologyManagementUri"
        }
    }
}
