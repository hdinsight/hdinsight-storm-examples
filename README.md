# hdinsight-storm-examples
This repository contains complete and easy to use examples that demonstrate the power of Apache Storm on Azure HDInsight.

## Getting Started
Each of the examples folder contains a ```build.bat``` that you can use to get started. 
It will first build all the required projects and then run the example (using ```run.bat```).

Running an example includes:
* Creation of required services (and resources) in Azure
* Preparing your topologies for submission (using ```prepare.ps1```)
* Submitting the topologies (using ```submit.ps1```) to the HDInsight Storm cluster

## Examples
There are multiple end-to-end examples included in this repository that showcase different topologies that you can build and run against HDInsight Storm.
HDInsight provides users the option to write applications in language of their choice and exposes Storm REST APIs as well as additional web interfaces for remote topology submission and management.

### Internet of Things Example (Java)
An internet of things example that demonstrates connected cars scenarios. 
The example showcases the reading of vehicle events from Event Hub, referencing Azure DocumentDB for vehicular data and then finally storing the computations in Azure Storage using the WASB driver on HDInsight clusters.

Read more about this example here: [IoT example using Azure EventHubs, Storm, Azure DocumentDB, Azure Storage/WASB](IotExample/README.md)

### EventHubs Scalability Example (Java)
A scalability benchmark that demonstrates throughput from EventHub and stores the event counts in SQL Azure.

Read more about this example here: [Event Count example using Azure EventHubs, Storm, SQL Azure](EventCountExample/README.md)

### Real Time ETL Example (SCP.Net)
A real time ETL examples that deploy two topologies.
The first one to generate Web Request Logs and push them to EventHub.
And, the second topology reads from EventHub, calculates aggregations("count() group by") and then stores the computations in a HDInsight HBase cluster.

Read more about this example here: [Real Time ETL example using Azure EventHubs, Storm, HBase & SCP.Net](RealTimeETLExample/README.md)

## Additional Resources
* Writing Storm applications in C# using [SCP.Net](SCPNet-GettingStarted.md)

## References
* [HDInsight] http://azure.microsoft.com/en-us/documentation/services/hdinsight/
* [Azure Event Hubs] (http://azure.microsoft.com/en-us/services/event-hubs/)
* [DocumentDB] (http://azure.microsoft.com/en-us/services/documentdb/)
* [SQL Azure] (http://azure.microsoft.com/en-us/services/sql-database/)