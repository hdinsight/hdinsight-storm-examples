[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-zA-Z0-9-]*$")]
    [ValidateLength(8,64)]
    [String]$ClusterName,                   # required    needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [String]$StorageAccount,                # required
    [Parameter(Mandatory = $true)]
    [String]$ContainerName,                 # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,               # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword,               # required
    [Parameter(Mandatory = $true)]          # required
    [String]$ClusterType,
    [Int]$ClusterSize = 4,                  # optional
    [Parameter(Mandatory = $false)]         # optional
    [String]$VNetId,
    [Parameter(Mandatory = $false)]         # optional
    [String]$SubnetName,
    [Parameter(Mandatory = $false)]         # optional
    [String]$ScriptActionUri,
    [Parameter(Mandatory = $false)]         # optional
    [String]$ScriptActionParameters
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

Write-SpecialLog "Create HDInsight cluster of type: $ClusterType with name: $ClusterName" (Get-ScriptName) (Get-ScriptLineNumber)
    
$Cluster = Get-AzureHDInsightCluster -Name $ClusterName

if($Cluster -ne $null)
{
    Write-SpecialLog ("Found an existing HDInsight cluster with same name: $ClusterName, Type: {0}, State: {1}" -f $Cluster.ClusterType,$Cluster.State) (Get-ScriptName) (Get-ScriptLineNumber)
    $startTime = Get-Date
    $finishTime = Get-Date
    $totalSeconds = ($finishTime - $startTime).TotalSeconds
    while(($totalSeconds -lt 3600) -and ($Cluster.State -ne [Microsoft.WindowsAzure.Management.HDInsight.ClusterState]::Running) -and 
    ($Cluster.State -ne [Microsoft.WindowsAzure.Management.HDInsight.ClusterState]::Operational))
    {
        sleep -s 15
        $Cluster = Get-AzureHDInsightCluster -Name $ClusterName
        $finishTime = Get-Date
        $totalSeconds = ($finishTime - $startTime).TotalSeconds
    }
}
else
{
    Write-InfoLog "Getting Storage Account properties" (Get-ScriptName) (Get-ScriptLineNumber)
    $StorageUrl = "${StorageAccount}.blob.core.windows.net"
    $StorageKey = Get-AzureStorageKey $StorageAccount  | %{ $_.Primary }
    $Location = Get-AzureStorageAccount $StorageAccount | %{ $_.Location }

    if([String]::IsNullOrWhitespace($VNetId) -or [String]::IsNullOrWhitespace($SubnetName))
    {
        Write-InfoLog "Creating HDInsight Cluster Configuration - Type: $ClusterType, Size: $ClusterSize, VNet: N/A" (Get-ScriptName) (Get-ScriptLineNumber)
        $Config = New-AzureHDInsightClusterConfig -ClusterSizeInNodes $ClusterSize -ClusterType $ClusterType
    }
    else
    {
        Write-InfoLog "Creating HDInsight Cluster Configuration - Type: $ClusterType, Size: $ClusterSize, VNet: $VNetId, Subnet: $SubnetName" (Get-ScriptName) (Get-ScriptLineNumber)
        $Config = New-AzureHDInsightClusterConfig -ClusterSizeInNodes $ClusterSize -ClusterType $ClusterType -VirtualNetworkId $VNetId -SubnetName $SubnetName
    }
	
    $Config = $Config | Set-AzureHDInsightDefaultStorage -StorageAccountName $StorageUrl -StorageAccountKey $StorageKey -StorageContainerName $ContainerName
    
    if($ClusterType.Equals("Storm", [System.StringComparison]::OrdinalIgnoreCase))
    {
        $Config = $Config | Add-AzureHDInsightConfigValues -Storm @{"supervisor.worker.timeout.secs"="45"}
    }

    if(-not ([String]::IsNullOrWhitespace($ScriptActionUri)))
    {
        Write-InfoLog "Adding Script Action: $ScriptActionUri" (Get-ScriptName) (Get-ScriptLineNumber)
        $Config = Add-AzureHDInsightScriptAction -Config $Config -Name "Customization" -ClusterRoleCollection HeadNode,DataNode -Uri $ScriptActionUri -Parameters $ScriptActionParameters
    }
    
    #Configure other config settings
    $SecurePwd = ConvertTo-SecureString $ClusterPassword -AsPlainText -Force
    $Creds = New-Object System.Management.Automation.PSCredential($ClusterUsername, $SecurePwd)

    Write-InfoLog "Creating Azure HDInsight $ClusterType cluster: $ClusterName" (Get-ScriptName) (Get-ScriptLineNumber)
    try
    {
        $Cluster = New-AzureHDInsightCluster -Name $ClusterName -Config $Config -Location "$Location" -Credential $Creds
    }
    catch
    {
        Write-ErrorLog ("Failed to create HDInsight cluster: {0}." -f $ClusterName)  (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw ("Failed to create HDInsight cluster: {0}." -f $ClusterName)
    }
}

$clusterInfo = $Cluster | Out-String
Write-InfoLog $clusterInfo (Get-ScriptName) (Get-ScriptLineNumber)

if(($Cluster.State -ne [Microsoft.WindowsAzure.Management.HDInsight.ClusterState]::Running) -and 
($Cluster.State -ne [Microsoft.WindowsAzure.Management.HDInsight.ClusterState]::Operational))
{
    Write-ErrorLog ("HDInsight Cluster: {0} is not in a running state. State: {1}" -f $ClusterName,$Cluster.State) (Get-ScriptName) (Get-ScriptLineNumber)
    throw ("HDInsight Cluster: {0} is not in a running state. State: {1}" -f $ClusterName,$Cluster.State)
}
return $Cluster