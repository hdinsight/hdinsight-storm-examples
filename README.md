# hdinsight-storm-examples
This repository contains complete and easy to use examples that demonstrate the power of Apache Storm on Azure HDInsight.

## Getting Started

### Running the examples
Each of the examples folder contains a ```run.bat``` that does the following:

1. Prepare (using ```prepare.ps1```)
  1. Creation of required services (and resources) in Azure
  2. Updating project properties and templates
2. Build (using ```build.ps1```)
  1. Building the source code and use the properties created in previous step
3. Execute (using ```execute.ps1```)
  1. Upload jars and create topology packages (as needed)
  2. Submitting the topologies to the HDInsight Storm cluster

An existing run configuration is first detected to provide idempotency in creation of Azure resources. This allows you to re-run the ```run.bat``` in case of some unexpected failure.

### Building the examples
If you just want to build the source code you can use the ```build.bat``` in each example folder to build that example.

### Re-running the example
If you modified some source and wish to re-deploy the topologies, you can build using ```build.bat``` and submit the topologies using ```execute.ps1```

## Examples
There are multiple end-to-end examples included in this repository that showcase different topologies that you can build and run against HDInsight Storm.
HDInsight provides users the option to write applications in language of their choice and exposes Storm REST APIs as well as additional web interfaces for remote topology submission and management.

### Internet of Things Example (Java)
An internet of things example that demonstrates connected cars scenarios. 
The example showcases the reading of vehicle events from Event Hub, referencing Azure DocumentDB for vehicular data and then finally storing the computations in Azure Storage using the WASB driver on HDInsight clusters.

Read more about this example here: [IoT example using Azure EventHubs, Storm, Azure DocumentDB, Azure Storage/WASB](IotExample/README.md)

### EventHubs Scalability Example (Java & SCP.Net)
A scalability benchmark that demonstrates throughput from EventHub and stores the event counts in SQL Azure.

Read more about this example here: [Event Count example using Azure EventHubs, Storm, SQL Azure](EventCountExample/README.md)

### Real Time ETL Example (SCP.Net)
A real time ETL examples that deploy two topologies.
The first one to generate Web Request Logs and push them to EventHub.
And, the second topology reads from EventHub, calculates aggregations("count() group by") and then stores the computations in a HDInsight HBase cluster.

Read more about this example here: [Real Time ETL example using Azure EventHubs, Storm, HBase & SCP.Net](RealTimeETLExample/README.md)

## More Examples
The repository now also has additional examples for your reference.

### Topology, Spout  Bolt templates
Templates to connect to various Azure services:
* EventHubs
* SQL Azure
* DocumentDB
* HBase
* Azure Websites (SignalR)

[Templates for Azure services](templates\templates.md)

### SCP.Net Examples
Old SCP.Net examples that were on HDInsight clusters have now been moved to this GitHub repository.

Please refer [More SCP.Net Examples](SCPNetExamples\SCPNetExamples.md)


## Additional Resources
* Writing Storm applications in C# using [SCP.Net](SCPNet-GettingStarted.md)

## References
* [HDInsight] (http://azure.microsoft.com/en-us/documentation/services/hdinsight/)
* [Azure Event Hubs] (http://azure.microsoft.com/en-us/services/event-hubs/)
* [DocumentDB] (http://azure.microsoft.com/en-us/services/documentdb/)
* [SQL Azure] (http://azure.microsoft.com/en-us/services/sql-database/)
