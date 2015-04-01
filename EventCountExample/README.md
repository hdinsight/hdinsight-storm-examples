# EventHubs scalability example (Java & SCP.Net Hybrid)
This is a scalability benchmark for HDInsight Storm clusters reading data from Azure EventHubs. 

The example contains three types of topologies:

1. EventGenTopology - An event sender topology that fills each EventHub partition with equal data.
2. EventCountTopology - A Java topology that reads the events from EventHub and stores the counts into SQL Azure database (uses TickTuples).
3. EventCountHybridTopology - A SCP.Net variation of the Java based EventCountTopology (uses TickTuples). 

This is the EventCountTopology data flow:

EventHubSpout --> PartialCountBolt --> DbGlobalCountBolt --> SQL Azure

PartialCountBolt counts the number of events using a tumbling window and for every 1000 messages it sends 1 tuple to DbGlobalCountBolt updating the global count. DbGlobalCountBolt updates SQL Azure database every 1 second.

We use an HTML page to connect to the same SQL Azure database and retrieve the data every 1 second, therefore we are viewing the real time count of received messages.

```IMPORTANT NOTES:```
* The example requires the user to change the "scale" of the service bus created for event hub to maximum throughput units (20). User will be prompted for the same during the example execution.
* The example deploys three topologies: EventGenTopology, EventCountDbTopology and optionally EventCountHybridTopology
* At any point in time, have only one topology running on the cluster to ensure that the subsequent topologies have enough worker slots to run on. The user will be prompted to kill the topologies during example execution.

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
