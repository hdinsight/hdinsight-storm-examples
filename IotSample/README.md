This is a sample for IOT (Internet of Things) scenario using Storm. The basic idea is to showcase how to use Storm topology to read messages from Azure EventHubs, read document from Azure DocumentDB for data referencing and save data to Azure Storage.

For this sample to work end to end, along with the Storm topology (iot), we have provided two tools:
1) EventHubs event generator (eventgen) 
2) DocumentDB document generator (docdbgen)

### Source code ###
vehiclegen.py: this is a simple python scripts to generate random vehicle VIN number and model number as a text file (vehiclevin.txt).

eventgen: the Azure EventHubs generator, a standalone java program to write messages to Azure EventHubs. It uses the vehicle info to generate engine temperature messages for each vehicle.

docdbgen: The Azure DocumentDB generator, a standalone java program to create document DB collection that stores vehicle VIN to model number map. Later the document DB is used to retrieve model number from the VIN.

iot: The Storm topology, it has the following components:
EventHubSpout: source of stream
TypeConversionBolt: converts EventHubs message in JSON format into fields in a tuple
DataReferenceBolt: enrich data by reading DocumentDB to get models for a given VIN
WasbStoreBolt: write the enriched data points into Azure Blob Storage

### Prerequisite ###

In order to build and run the demo, you need to have:
1) Java 1.7 SDK
2) Maven 3.x
3) Run Set-ExecutionPolicy Unrestricted in a admin powershell window

The access to Windows Azure Blob Storage requires hadoop-azure jar, which can only be built from Apache hadoop trunk at this point (it will be included in Hadoop 2.7). You can choose to build yourself, or you can use the prebuilt jar included in the lib folder.

### How to build ###
Run this script in Azure Powershell Window:
.\build_iotsample.ps1

This will first create all dependencies (DocumentDB, EventHubs, Storage) in Azure, then update configuration files, then build all 3 projects.

During the process, you will be prompted to login with your Azure credentials and type your subscription name.

### How to run ###
Run this script in Azure Powershell Window:
.\run_iotsample.ps1

This script will open a browser window for the external Storm UI, you need to enter Storm cluster credentials, they are stored at config\configurations.properties

Note, in the case of failure in creating resources in Azure, you can run ".\delete_resources_in_azure.ps1" which will delete resources that have been created.

### How to clean and delete all resources in Azure ###
Run this script in Azure Powershell Window:
.\delete_resources_in_azure.ps1
