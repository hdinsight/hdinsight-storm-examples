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
    [String]$HDInsightClusterType,
    [Int]$ClusterSize = 4,                  # optional
    [Parameter(Mandatory = $false)]         # optional
    [String]$VNetId,
    [Parameter(Mandatory = $false)]         # optional
    [String]$SubnetName
    )

###########################################################
# Start - Initialization - Invocation, Logging etc
###########################################################
$VerbosePreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath

& "$scriptDir\..\..\..\init.ps1"
if(-not $?)
{
    throw "Initialization failure."
    exit -9999
}
###########################################################
# End - Initialization - Invocation, Logging etc
###########################################################

Write-SpecialLog "Create Kafka cluster on HDInsight cluster of type: $HDInsightClusterType with name: $ClusterName" (Get-ScriptName) (Get-ScriptLineNumber)

$unzipUri = & "$scriptDir\..\..\Storage\UploadFileToStorage.ps1" $StorageAccount "kafkaconfigactionv03" "$scriptDir\unzip.exe" "unzip.exe"
Write-InfoLog "unzipUri: $unzipUri" (Get-ScriptName) (Get-ScriptLineNumber)
$kafkaVersion = "kafka_2.11-0.8.2.1"
$kafkaUri = & "$scriptDir\..\..\Storage\UploadFileToStorage.ps1" $StorageAccount "kafkaconfigactionv03" "$scriptDir\$kafkaVersion.zip" "$kafkaVersion.zip"
Write-InfoLog "KafkaUri: $kafkaUri" (Get-ScriptName) (Get-ScriptLineNumber)
$ScriptActionUri = & "$scriptDir\..\..\Storage\UploadFileToStorage.ps1" $StorageAccount "kafkaconfigactionv03" "$scriptDir\kafka-installer-v03.ps1" "kafka-installer-v03.ps1"
Write-InfoLog "ScriptActionUri: $ScriptActionUri" (Get-ScriptName) (Get-ScriptLineNumber)
$ScriptActionParameters = "-KafkaBinaryZipLocation $kafkaUri -KafkaHomeName $kafkaVersion -UnzipExeLocation $unzipUri -RemoteAdminUsername remote{0} -RemoteAdminPassword {1}" -f $ClusterUsername, $ClusterPassword
Write-InfoLog "ScriptActionParameters: $ScriptActionParameters" (Get-ScriptName) (Get-ScriptLineNumber)
if($VNetId -and $SubnetName)
{
    $Cluster = & "$scriptDir\..\CreateCluster.ps1" $ClusterName $StorageAccount $ContainerName $ClusterUsername $ClusterPassword $HDInsightClusterType $ClusterSize $VNetId $SubnetName $ScriptActionUri $ScriptActionParameters
}
else
{
    $Cluster = & "$scriptDir\..\CreateCluster.ps1" $ClusterName $StorageAccount $ContainerName $ClusterUsername $ClusterPassword $HDInsightClusterType $ClusterSize "" "" $ScriptActionUri $ScriptActionParameters
}

return $Cluster