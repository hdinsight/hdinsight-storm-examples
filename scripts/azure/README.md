#Azure PowerShell Helper Scripts
All the end-to-end examples in this repository use these scripts to create various Azure resources. The newer scripts target Azure Resource Manager to create the required resources.
You should be able to use them in any of your automation scenarios, just ensure that you also copy the init & logging modules alongwith these scripts.

Init module: [../init.ps1](../init.ps1)

Logging module: [../logging/Logging-HDInsightExamples.psm1](../logging/Logging-HDInsightExamples.psm1)

Azure Services:
* [DocumentDB](DocumentDB)
* [EventHubs](EventHubs)
* [HDInsight](HDInsight)
* [SqlAzure](SqlAzure)
* [Storage](Storage)
* [VirtualNetwork](VirtualNetwork)

The scripts ```CreateAzureResources.ps1``` works off the run\configuration.properties created during an end-to-end example execution.
It uses the above mentioned scripts to create all the desired Azure resources.
