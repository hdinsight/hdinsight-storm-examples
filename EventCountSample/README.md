This is a scalability benchmark for HDInsight Storm clusters reading data from Azure EventHubs. This is the topology:

EventHubSpout --> PartialCountBolt --> DbGlobalCountBolt --> SQL Azure

Partial count bolt count the number of events using a tumbling window and for every 1000 messages it sends 1 tuple to DbGlobalCountBlot updating the global count. DbGlobalCountBolt updates SQL Azure database every 1 second.

We use an HTML page to connect to the same SQL Azure database and retrieve the data every 1 second, therefore we are viewing the real time count of received messages.

We also use the same Storm cluster to populate EventHubs messages.

### Prerequisite ###
In order to build and run the demo, you need to have:
1) Java 1.7 SDK
2) Maven 3.x
3) Run Set-ExecutionPolicy Unrestricted in a admin powershell window
  
### How to build ###
    build_eventcount.ps1

### How to run ###
    run_eventcount.ps1
    
### How to clean and delete all resources in Azure ###
    delete_azure_resources.ps1