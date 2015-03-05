[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$ClusterUrl,
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword
)

$secureClusterPassword = ConvertTo-SecureString $ClusterPassword -AsPlainText -Force
$clusterCreds = New-Object System.Management.Automation.PSCredential ($ClusterUsername, $secureClusterPassword)

$clusterUri = new-object Uri($ClusterUrl)
$stormUiUri = new-object Uri($clusterUri, "StormDashboard/StormExtUI")

Write-SpecialLog ("Launching browser for Storm dashboard: " + $stormUiUri.AbsoluteUri) (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog "Please use the following credentials in your browser window:" (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog ("Storm Cluster Username: " + $ClusterUsername) (Get-ScriptName) (Get-ScriptLineNumber)
Write-InfoLog ("Storm Cluster Password: " + $ClusterPassword) (Get-ScriptName) (Get-ScriptLineNumber)

$result = & "start" $stormUiUri