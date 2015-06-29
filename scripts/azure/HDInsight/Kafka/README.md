# Apache Kafka on Azure HDInsight Storm/Spark Cluster

This directory contains Azure HDInsight ScriptAction scripts to install Kafka any type of HDInsight cluster: Storm, HBase, Spark, Hadoop.

Directory contents:
* [CreateKafkaCluster.ps1](CreateKafkaCluster.ps1) - A script that invokes the [HDInsight CreateCluster](..\CreateCluster.ps1) script to deploy a Kafka cluster.
* [kafka-installer-v02.ps1](kafka-installer-v02.ps1) - The main script that is passed to HDInsight Cluster Creation command to be invoked during cluster creation.
```PowerShell
$ScriptActionUri = & "..\Storage\UploadFileToStorage.ps1" --AccountName $StorageAccountName "kafkaconfigactionv02" ".\Kafka\kafka-installer-v02.ps1" "kafka-installer-v02.ps1"
$ScriptActionParameters = "-KafkaBinaryZipLocation $kafkaUri -KafkaHomeName $kafkaVersion -UnzipExeLocation $unzipUri -RemoteAdminUsername remote{0} -RemoteAdminPassword {1}" -f $ClusterUsername, $ClusterPassword
```
* [kafka_2.11-0.8.2.1.zip](kafka_2.11-0.8.2.1.zip) - Latest Kafka 2.11-0.8.2.1 bits downloaded from [http://kafka.apache.org/downloads.html](http://kafka.apache.org/downloads.html). The tar file is extracted and re-packaged into a zip file alongwith with [nssm](http://www.nssm.cc/) to install Kafka as a service.
* [HDInsightUtilities-v02.psm1](HDInsightUtilities-v02.psm1) - HDInsight Utilities Script that provides general functions that you can use during execution. The script is hosted on [https://hdiconfigactions.blob.core.windows.net/configactionmodulev02/HDInsightUtilities-v02.psm1](https://hdiconfigactions.blob.core.windows.net/configactionmodulev02/HDInsightUtilities-v02.psm1)
* [unzip.exe](unzip.exe) - A simple command line tool to zip or extract files. We use this as a more reliable way of extraction than using Shell.Application (via Expand-HDIZippedFile).

## Create Kafka Cluster
```PowerShell
$cluster = & ".\Kafka\CreateKafkaCluster.ps1" --ClusterName $ClusterName --StorageAccount $StorageAccountName --ContainerName $StorageContainerName --ClusterUsername $ClusterUsername --ClusterPassword $ClusterPassword --HDInsightClusterType "Storm" --ClusterSize $ClusterSize --VNetId $VNetId --SubnetName $SubnetName
```

## Kafka Installation Script Notes

* The RemoteAdminUsername and RemoteAdminPassword are optional but come in very handy if you want administrative access created on each node on your cluster. Once you enable RDP on your cluster, you can use this username/password to login via admin credentials. Please use this at your own discretion to avoid giving more than required access.
* The ```nssm.exe``` ensures that your Kafka service is always up.
* The script is idempotent that is it will check if your service is up and running, if it is not, it will attempt installation. The BrokerId is consistent for that particular node.

## Kafka Quick Start

The above topic takes care of starting the Kafka server on each node with its own respective BrokerId. It also updates all the properties files with the Zookeeper quorum from the HDInsight cluster.
The head nodes: headnode0 and headnode1 get BrokerId 0 and 1, while all the worker nodes get BrokerId numerically increasing after that (workernode0 gets BrokerId 2 and so on).

You can get the Zookeeper quorum by accessing the ```zookeeper.connect``` value from ```$ENV:KAFKA_HOME\config\server.properties``` file.

```[SPECIAL NOTE]:``` If you have only one cluster you can use zookeeper0:2181,zookeeper1:2181,zookeeper2:2181 as your quorum. But if you have deployed multiple HDInsight clusters in the same VNet (most likely the case), you should use the FQDN - full dns names for the Zookeepers as found in the aforementioned file.

1. Create a topic
  ```
  %KAFKA_HOME%\bin\windows\kafka-topics.bat --create --zookeeper zookeeper0:2181,zookeeper1:2181,zookeeper2:2181 --replication-factor 1 --partitions 1 --topic test
  ```
2. List the topics
  ```
  %KAFKA_HOME%\bin\windows\kafka-topics.bat --list --zookeeper zookeeper0:2181,zookeeper1:2181,zookeeper2:2181
  ```
3. Send some messages
  ```
  %KAFKA_HOME%\bin\windows\kafka-console-producer.bat --broker-list headnode0:9092,headnode1:9092,localhost:9092 --topic test
  ```
  Type in some messages like:
  ```
  This is a message
  This is another message
  ```
  When done, press Ctrl+C to exit.
4. Start a consumer
  ```
  %KAFKA_HOME%\bin\windows\kafka-console-consumer.bat --zookeeper zookeeper0:2181,zookeeper1:2181,zookeeper2:2181 --topic test --from-beginning
  ```
  When done, press Ctrl+C to exit.

## Kafka Replication Factor
Some additional notes w.r.t. replication in Kafka [http://kafka.apache.org/documentation.html#replication](http://kafka.apache.org/documentation.html#replication)

A HDInsight cluster is spread across 5 Upgrade domains and 2 Fault Domains. On a Azure OS update roll out 20% (1/5th) of your nodes could be unavailable.
This does not impact any existing offered cluster types as they use Windows Azure Blobs (WASB) as their default storage, so your data is never gone even if a node was unavailable.

However for Kafka, this installation script does not use WASB as the storage platform for Kafka. You may want to consider increasing the replication factor to accomplish durability.
To ensure your data is highly available during such events, the most safest and conservative approach would be to have replication factor greater than 20% of your node size i.e. a replication factor of 3 is good until a cluster size of 15 nodes.

Again - this comment is only applicable considering this custom script action in perspective as we have not touched anything related to Kafka FS in this script.

## How to debug custom script action

For a working script things are rosy and you get all the output. But when you have a buggy script things can get a little hard. 
You need to check the ```SetupLog``` Azure Table created in your storage account for the TraceLevel == "Error" logs to show what failed in your script.

* It is a good idea to have most commonly failing things in try catch and log extensively to ensure things are working to your expectations.
* It is also a good idea to first create a cluster and then try to run your scripts via the RDP user (by omitting out any admin related actions) to ensure sanity.
* One handy way to validate your existing PowerShell script for syntax or parsing errors is to load it as a script block in PowerShell to verify correctness. Nothing is more frustrating than waiting 15 minutes to find a syntax error in your script.
  * Here is an example script that takes your script's full path as an input and let's you know if it has any issues: [../../VerifyPowershellScript.ps1](../../VerifyPowershellScript.ps1)

## References
* [HDInsightUtilities-v02.psm1](https://hdiconfigactions.blob.core.windows.net/configactionmodulev02/HDInsightUtilities-v02.psm1)
* [PowerShell helper functions for Azure HDInsight](https://github.com/Blackmist/hdinsight-tools)
