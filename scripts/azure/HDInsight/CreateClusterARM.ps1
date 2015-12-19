[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-zA-Z0-9-]*$")]
    [ValidateLength(8,64)]
    [String]$ResourceGroupName,             # required    needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [String]$Location,                      # required
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
    [Parameter(Mandatory = $true)]          # required
    [String]$OSType,
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

& "$scriptDir\..\CreateAzureResourceGroup.ps1" $ResourceGroupName $Location

Write-SpecialLog "Create HDInsight cluster of type: $ClusterType with name: $ClusterName" (Get-ScriptName) (Get-ScriptLineNumber)

$Cluster = Get-AzureRmHDInsightCluster | ? { $_.Name -eq $ClusterName }

if($Cluster -eq $null)
{
    Write-InfoLog "Getting Storage Account properties" (Get-ScriptName) (Get-ScriptLineNumber)
    $sa = Get-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName -Name $StorageAccount
    $StorageBlobUrl = $sa.PrimaryEndpoints.Blob.DnsSafeHost
    $StorageKey = Get-AzureRmStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $StorageAccount  | %{ $_.Key1 }   # Fetch list and get the account & resource group name

    $Config = New-AzureRmHDInsightClusterConfig -ClusterType $ClusterType -DefaultStorageAccountName $StorageBlobUrl -DefaultStorageAccountKey $StorageKey
    
    if($ClusterType.Equals("Storm", [System.StringComparison]::OrdinalIgnoreCase))
    {
        $Config = $Config | Add-AzureRmHDInsightConfigValues -Storm @{"supervisor.worker.timeout.secs"="45"}
    }

    if(-not ([String]::IsNullOrWhitespace($ScriptActionUri)))
    {
        Write-InfoLog "Adding Script Action: $ScriptActionUri" (Get-ScriptName) (Get-ScriptLineNumber)
        $Config = Add-AzureRmHDInsightScriptAction -Config $Config -Name "Customization" -ClusterRoleCollection HeadNode,DataNode -Uri $ScriptActionUri -Parameters $ScriptActionParameters
    }
    
    #Configure other config settings
    $httpCredential = New-Object System.Management.Automation.PSCredential($ClusterUsername, ($ClusterPassword | ConvertTo-SecureString -AsPlainText -Force))
    $sshCredential = New-Object System.Management.Automation.PSCredential(("ssh" + $ClusterUsername), ($ClusterPassword | ConvertTo-SecureString -AsPlainText -Force))
    
    Write-InfoLog "Creating Azure HDInsight $ClusterType cluster: $ClusterName" (Get-ScriptName) (Get-ScriptLineNumber)
    try
    {
        if($OSType -eq "Linux")
        {
            if($VNetId -and $SubnetName)
            {
                $Cluster = New-AzureRmHDInsightCluster -ResourceGroupName $ResourceGroupName -ClusterName $ClusterName -Config $Config -DefaultStorageContainer $ContainerName -Location "$Location" `
                    -ClusterSizeInNodes $ClusterSize -HttpCredential $httpCredential -ClusterType $ClusterType -SshCredential $sshCredential -OSType $OSType `
                    -VirtualNetworkId $VNetId -SubnetName ($VNetId + "/subnets/" + $SubnetName)
            }
            else
            {
                $Cluster = New-AzureRmHDInsightCluster -ResourceGroupName $ResourceGroupName -ClusterName $ClusterName -Config $Config -DefaultStorageContainer $ContainerName -Location "$Location" `
                    -ClusterSizeInNodes $ClusterSize -HttpCredential $httpCredential -ClusterType $ClusterType -SshCredential $sshCredential -OSType $OSType
            }
        }
        else
        {
            if($VNetId -and $SubnetName)
            {
                $Cluster = New-AzureRmHDInsightCluster -ResourceGroupName $ResourceGroupName -ClusterName $ClusterName -Config $Config -DefaultStorageContainer $ContainerName -Location "$Location" `
                    -ClusterSizeInNodes $ClusterSize -HttpCredential $httpCredential -ClusterType $ClusterType -OSType $OSType `
                    -VirtualNetworkId $VNetId -SubnetName $SubnetName
            }
            else
            {
                $Cluster = New-AzureRmHDInsightCluster -ResourceGroupName $ResourceGroupName -ClusterName $ClusterName -Config $Config -DefaultStorageContainer $ContainerName -Location "$Location" `
                    -ClusterSizeInNodes $ClusterSize -HttpCredential $httpCredential -ClusterType $ClusterType -OSType $OSType                
            }
        }
    }
    catch
    {
        Write-ErrorLog ("Failed to create HDInsight cluster: {0}." -f $ClusterName)  (Get-ScriptName) (Get-ScriptLineNumber) $_
        throw ("Failed to create HDInsight cluster: {0}." -f $ClusterName)
    }
}

$clusterInfo = $Cluster | Out-String
Write-InfoLog $clusterInfo (Get-ScriptName) (Get-ScriptLineNumber)

$startTime = Get-Date
while(($totalSeconds -lt 3600) -and `
    (($Cluster.ClusterState -ne "Running") -and ($Cluster.ClusterState -ne "Operational")) -and 
    (-not [String]::IsNullOrWhitespace($Cluster.Error)))
{
    sleep -s 30
    $Cluster = Get-AzureRmHDInsightCluster -ResourceGroupName $ResourceGroupName -ClusterName $ClusterName
    Write-InfoLog ("HDInsight Cluster: $ClusterName, Type: {0}, State: {1}" -f $Cluster.ClusterType, $Cluster.ClusterState) (Get-ScriptName) (Get-ScriptLineNumber)
    $finishTime = Get-Date
    $totalSeconds = ($finishTime - $startTime).TotalSeconds
    if(($Cluster.ClusterState -like "Error") -or ($Cluster.ClusterState -like "Deleting"))
    {
      Write-ErrorLog ("HDInsight Cluster: $ClusterName is in {0} state. Please check the error message, delete and retry again." -f $Cluster.ClusterState) (Get-ScriptName) (Get-ScriptLineNumber)
      break
    }
}

if (($Cluster.ClusterState -ne "Running") -and ($Cluster.ClusterState -ne "Operational"))
{
    Write-ErrorLog ("HDInsight Cluster: {0} is not in a running/operational state. State: {1}" -f $ClusterName, $Cluster.ClusterState) (Get-ScriptName) (Get-ScriptLineNumber)
    throw ("HDInsight Cluster: {0} is not in a running/operational state. State: {1}" -f $ClusterName, $Cluster.ClusterState)
}
return $Cluster