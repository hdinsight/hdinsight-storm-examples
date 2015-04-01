[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ClusterUrl,                           # required
    [Parameter(Mandatory = $true)]
    [String]$TopologyName,                         # required
    [Parameter(Mandatory = $true)]
    [String]$TopologyAction,                       # required
    [String]$TopologyActionWait                    # optional
    )

#Reference: https://github.com/apache/storm/blob/master/STORM-UI-REST-API.md

$clusterUri = new-object Uri($ClusterUrl)

#Get topologies
$topologySummaryUri = new-object Uri($clusterUri, "api/v1/topology/summary")
Write-Host ("Sending GET request at: " + $topologySummaryUri)
try
{
    $response = Invoke-RestMethod -Uri $topologySummaryUri -Method Get
    Write-Host ("Response:`r`n" + ($response | Out-String))

    $topologyId = ($response.topologies | ? { $_.name -like $TopologyName } | Select-Object -Last 1).id

    if([String]::IsNullOrWhitespace($topologyId))
    {
        Write-Error "No topologyId found in response for $TopologyName."
        throw "No topologyId found in response for $TopologyName."
    }
    else
    {
        Write-Host "Topology: $TopologyName - topologyId = $topologyId"
    }
}
catch
{
    Write-Error "Exception encountered while invoking the [GET] rest method at: $topologySummaryUri" $_
    throw "Exception encountered while invoking the [GET] rest method at: $topologySummaryUri"
}

#Get anti forgery token
$topologyUri = new-object Uri($clusterUri, ("api/v1/topology/{0}" -f $topologyId))
Write-Host ("Sending GET request at: " + $topologyUri)
try
{
    $response = Invoke-RestMethod -Uri $topologyUri -Method Get -SessionVariable webSession
    Write-Host ("Response:`r`n" + ($response | Out-String))
    $antiForgeryToken=$response.antiForgeryToken
    if([String]::IsNullOrWhitespace($antiForgeryToken))
    {
        Write-Error "No antiForgeryToken found in response."
        throw "No antiForgeryToken found in response."
    }
    else
    {
        Write-Host "Topology: $TopologyName - antiForgeryToken = $antiForgeryToken"
    }
}
catch
{
    Write-Error "Exception encountered while invoking the [GET] rest method at: $topologyUri" $_
    throw "Exception encountered while invoking the [GET] rest method at: $topologyUri"
}

if($TopologyAction -like "get")
{
    Write-Host "$TopologyName - Spouts"
    Write-Host ("Spouts" + ($response.spouts | Out-String))
    Write-Host "$TopologyName - Bolts"
    Write-Host ("Bolts" + ($response.bolts | Out-String))
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
        $topologyManagementUri = new-object Uri($clusterUri, ("api/v1/topology/{0}/{1}" -f $topologyId,$TopologyAction))
    }
    else
    {
        $topologyManagementUri = new-object Uri($clusterUri, ("api/v1/topology/{0}/{1}/{2}" -f $topologyId,$TopologyAction,$TopologyActionWait))
    }

    Write-Host ("Sending POST request at: " + $topologyManagementUri)
    try
    {
        $response = Invoke-RestMethod -Uri $topologyManagementUri -Method Post -Headers @{"x-csrf-token"=$antiForgeryToken} -WebSession $webSession
        Write-Host "Topology action '$TopologyAction' complete: $ClassName"
        Write-Host ("Response:`r`n" + ($response | Out-String))
    }
    catch
    {
        if($_.Exception.Response.StatusCode -eq [System.Net.HttpStatusCode]::Unauthorized)
        {
            Write-Host $_ $_
            Write-Host "Known issue: Received a 401 status code from Storm Rest. Ignoring the exception." $_
        }
        else
        {
            Write-Error "Exception encountered while invoking the [POST] rest method at: $topologyManagementUri" $_
            throw "Exception encountered while invoking the [POST] rest method at: $topologyManagementUri"
        }
    }
}
