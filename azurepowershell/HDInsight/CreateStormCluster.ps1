[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-zA-Z0-9-]*$")]
    [String]$ClusterName,                    # required    needs to be alphanumeric or "-"
    [Parameter(Mandatory = $true)]
    [String]$StorageAccount,                 # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterUsername,                # required
    [Parameter(Mandatory = $true)]
    [String]$ClusterPassword,                # required
    [Int]$ClusterSize = 4                    # optional
    )

$ClusterType = "Storm"

Write-Host "Getting Storage Account properties"
$StorageUrl = "${StorageAccount}.blob.core.windows.net"
$StorageKey = Get-AzureStorageKey $StorageAccount  | %{ $_.Primary }
$ContainerName = $ClusterName
$Location = Get-AzureStorageAccount $StorageAccount | %{ $_.Location }

Write-Host "Generating Configurations"
$Config = New-AzureHDInsightClusterConfig -ClusterSizeInNodes $ClusterSize -ClusterType $ClusterType
$Config = $Config | Set-AzureHDInsightDefaultStorage -StorageAccountName $StorageUrl -StorageAccountKey $StorageKey -StorageContainerName $ContainerName
$Config = $Config | Add-AzureHDInsightConfigValues -Storm @{"supervisor.worker.timeout.secs"="45"}

#Configure other config settings
$SecurePwd = ConvertTo-SecureString $ClusterPassword -AsPlainText -Force
$Creds = New-Object System.Management.Automation.PSCredential($ClusterUsername, $SecurePwd)

Write-Host "Creating Azure HDInsight $ClusterType cluster [$ClusterName]"
$Cluster = New-AzureHDInsightCluster -Name $ClusterName -Config $Config -Location "$Location" -Credential $Creds