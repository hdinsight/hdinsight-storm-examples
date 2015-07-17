# SCP.Net Examples
This folder contains the original SCP.Net examples the details the different concepts of SCP.Net and usage patterns.

If you are looking for Azure Services related templates look into this directory: [HDInsight Storm Azure Application Templates] (../templates)

## Building the examples
Build the examples using ```build.bat``` in this directory.

## Example types
1. **TopologyBuilder**: Following examples use TopologyBuilder and SCPHost.exe as the executable container. They can be easily submitted to the cluster using HDInsight VS Tools.
  1. HelloWorldHostMode 
  2. HelloWorldHostModeMultiSpout
  3. HybridTopologyHostMode

2. **Executable**: Following examples show executable based invocation i.e. one has to use the provided spec files or create one of their own.
  1. HelloWorld
  2. HelloWorldKafka
  3. HybridTopology

You can also choose to use the alternative SetSpout & SetBolt methods of the TopologyBuilder if you want to use executables as an application type:
```csharp
public SpoutDeclarer SetSpout(string spoutName, string pluginName, List<string> pluginArgs, Dictionary<string, List<string>> outputSchema, int parallelismHint, bool enableAck = false)
```
```csharp
public BoltDeclarer SetBolt(string boltName, string pluginName, List<string> pluginArgs, Dictionary<string, List<string>> outputSchema, int parallelismHint, bool enableAck = false)
```

## How to submit SCPNetExamples to the cluster using SCPAPI (web)

1. Build the examples using ```build.bat``` in this directory.

2. Go to the respective example folder like ```HelloWorld``` and run the following command 

3. [OPTIONAL] Create the SCP.Net spec using the topology dll, applicable to **TopologyBuilder** based examples. Make sure you set the Active attribute on your Topology or provide the fully qualified class name.
```
..\..\scripts\scpnet\CreateScpSpec.ps1 -TopologyAssembly .\bin\Debug\HelloWorldHostMode.dll -SpecFile HelloWorldHostMode.spec
```

3. Create the SCP.Net package using the binaries directory: 
```
..\..\scripts\scpnet\CreateScpPackage.ps1 -TopologyAssemblyDir .\bin\Debug -PackageFile HelloWorld.zip
```
For a Hybrid topology like ```HybridTopology``` provide the Java dependencies folder
```
..\..\scripts\scpnet\CreateScpPackage.ps1 -TopologyAssemblyDir .\bin\Debug -PackageFile HelloWorld.zip -JavaDependencies .\java\target\HybridTopology-1.0-SNAPSHOT.jar
```

4. Submit the topology using the following command: 
```
..\..\scripts\scpnet\SubmitSCPNetTopology.ps1 -ClusterUrl $clusterUrl -ClusterUsername $clusterUsername -ClusterPassword $clusterPassword -SpecFile .\HelloWorld.spec -PackageFile .\HelloWorld.zip
```

## How to submit SCPNetExamples on the cluster (manually)
This approach is useful for **Executable** type of topologies or when you are hitting issues submitting the topologies from SCPAPI.

1. Build the examples using ```build.bat``` in this directory.

2. Go to the respective example folder like ```HelloWorld``` and copy the spec file like ```HelloWorld.spec``` and the binaries directory like ```HelloWorld\bin\debug``` to your Storm cluster

3. Open the ```Storm Command Line``` shortcut on your Storm cluster desktop

4. Submit the topology using the following command (NOTE: Do not forget to specify the specs agrument as that is the temp directory where spec compilation takes place): 
```
bin\runSpec.cmd MyExample\HelloWorld.spec specs MyExample\bin\Debug
```
Output:
```
C:\apps\dist\storm-0.9.3.2.2.6.1-0011>bin\runSpec.cmd MyExample\HelloWorld.spec specs MyExample\bin\Debug
Version: 0.9.4.124
Compiling MyExample\HelloWorld.spec...
Submitting topology HelloWorld...
```

## Cleaning the examples
Clean the examples using ```cleanup.bat``` in this directory.