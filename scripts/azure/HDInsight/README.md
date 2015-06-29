# Azure HDInsight Cluster Creation Scripts

This directory contains Azure PowerShell scripts to create any type of HDInsight cluster: Storm, HBase, Spark, Hadoop.

The Kafka directory contains additional scripts on how to deploy ```Kafka``` on a HDInsight Storm or a Spark cluster. You can read more about the Kafka installation here in [Kafka Install Notes](Kafka)

The scripts demonstrate how you can:
* Provide a VNetID (GUID) and a Subnet Name at cluster creation to deploy all clusters and your services in same Azure Virtual Network (v1).
* Provide a custom script action through which you can install any third party components. The scripts actions are run under administrative privileges during cluster creation (or on any node re-image to ensure your components are always available on the cluster).

## How to use the scripts

* You can deploy any HDInsight cluster type with these scripts by providing values like ```Storm```, ```Hadoop```, ```HBase```, ```Spark``` to the same script. 
  * The (CreateCluster.ps1)[CreateCluster.ps1] script currently supports only Windows OS type, the support for Linux will be soon added to these scripts.

* [OPTIONAL] All the scripts use custom logging functions to help provide the information what is executing and where it is executing. The scripts will automatically import the logging module via the initialization script located at [../init.ps1](../init.ps1)
  * If you decide to use these scripts outside of this repository, you can either:
    * Copy the logging module and import it manually from [../logging/Logging-HDInsightExamples.psm1](../logging/Logging-HDInsightExamples.psm1)
    ```PowerShell
    Import-Module "..\..\logging\Logging-HDInsightExamples.psm1" -Force
    ```
    * Or, replace all the Write-InfoLog, Write-SpecialLog & Write-ErrorLog with functions of your choice.

### HDInsight Storm Cluster

```PowerShell
$cluster = & ".\CreateCluster.ps1" --ClusterName $ClusterName --StorageAccount $StorageAccountName --ContainerName $StorageContainerName --ClusterUsername $ClusterUsername --ClusterPassword $ClusterPassword --ClusterType "Storm" --ClusterSize $ClusterSize --VNetId $VNetId --SubnetName $SubnetName
```

### HDInsight HBase Cluster

```PowerShell
$cluster = & ".\CreateCluster.ps1" --ClusterName $ClusterName --StorageAccount $StorageAccountName --ContainerName $StorageContainerName --ClusterUsername $ClusterUsername --ClusterPassword $ClusterPassword --ClusterType "HBase" --ClusterSize $ClusterSize --VNetId $VNetId --SubnetName $SubnetName
```

### HDInsight Spark Cluster

```PowerShell
$cluster = & ".\CreateCluster.ps1" --ClusterName $ClusterName --StorageAccount $StorageAccountName --ContainerName $StorageContainerName --ClusterUsername $ClusterUsername --ClusterPassword $ClusterPassword --ClusterType "Spark" --ClusterSize $ClusterSize --VNetId $VNetId --SubnetName $SubnetName
```

### Kafka on HDInsight Storm Cluster
You can read more about the Kafka installation here in [Kafka Install Notes ReadMe](Kafka\README.md)

```PowerShell
$unzipUri = & "..\Storage\UploadFileToStorage.ps1" --AccountName $StorageAccountName --ContainerName "kafkaconfigactionv02" --FilePath ".\Kafka\unzip.exe" "unzip.exe"
$kafkaVersion = "kafka_2.11-0.8.2.1"
$kafkaUri = & "..\Storage\UploadFileToStorage.ps1" --AccountName $StorageAccountName --ContainerName "kafkaconfigactionv02" --FilePath "$.\Kafka\$kafkaVersion.zip" "$kafkaVersion.zip"
$ScriptActionUri = & "..\Storage\UploadFileToStorage.ps1" --AccountName $StorageAccountName "kafkaconfigactionv02" ".\Kafka\kafka-installer-v02.ps1" "kafka-installer-v02.ps1"
$ScriptActionParameters = "-KafkaBinaryZipLocation $kafkaUri -KafkaHomeName $kafkaVersion -UnzipExeLocation $unzipUri -RemoteAdminUsername remote{0} -RemoteAdminPassword {1}" -f $ClusterUsername, $ClusterPassword
$cluster = & ".\CreateCluster.ps1" --ClusterName $ClusterName --StorageAccount $StorageAccountName --ContainerName $StorageContainerName --ClusterUsername $ClusterUsername --ClusterPassword $ClusterPassword --ClusterType "Storm" --ClusterSize $ClusterSize --VNetId $VNetId --SubnetName $SubnetName --ScriptActionUri $ScriptActionUri --$ScriptActionParameters $ScriptActionParameters
```

-OR-
```PowerShell
$cluster = & ".\Kafka\CreateKafkaCluster.ps1" --ClusterName $ClusterName --StorageAccount $StorageAccountName --ContainerName $StorageContainerName --ClusterUsername $ClusterUsername --ClusterPassword $ClusterPassword --HDInsightClusterType "Storm" --ClusterSize $ClusterSize --VNetId $VNetId --SubnetName $SubnetName
```

To deploy Kafka on a Spark cluster, just change the ClusterType value to "Spark".
