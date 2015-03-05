# EventHubs scalability example
This is a scalability benchmark for HDInsight Storm clusters reading data from Azure EventHubs. This is the topology:

EventHubSpout --> PartialCountBolt --> DbGlobalCountBolt --> SQL Azure

Partial count bolt count the number of events using a tumbling window and for every 1000 messages it sends 1 tuple to DbGlobalCountBlot updating the global count. DbGlobalCountBolt updates SQL Azure database every 1 second.

We use an HTML page to connect to the same SQL Azure database and retrieve the data every 1 second, therefore we are viewing the real time count of received messages.

## Prerequisites
In order to build and run the example, you need to have:
1. Java 1.7/1.8 SDK.
2. Maven 3.x - If you have M2_HOME configured, the examples should detect your mvn automatically.
3. Latest version of Azure Powershell (0.8.13 or later).
4. An active Azure subscription for deploying Azure services for example execution.
  
## How to build
Use ```EventCountExample\build.bat``` to build the example.

## How to run
Use ```EventCountExample\run.bat``` to run the example that will create resources in Azure, build and submit the example topology.

## How to clean and delete all resources in Azure and local build artifacts ###
Use ```EventCountExample\cleanup.bat``` to delete the resources created from previous step and also any local build generated artifacts.