# HDInsight Storm SCP.Net C# Templates
This project contains templates for spouts, bolts and topologies to interact with various Azure services like EventHub, DocumentDB, SQL Azure.

These templates will be available through the HDInsight Tools for Visual Studio.

Please follow the //TODO comments to setup your input and output schemas correctly between the spouts and bolts.

You can also use a pre-exisiting run configuration (run\configuraton.properties) from any of the examples folder to use with this templates solution.

This solution currently uses to be released SCP.Net SDK which will come with HDInsight Service Update 4 at //build 2015.

## Microsoft.SCP.Net.SDK - Upcoming changes in NuGet package
### New Features
1. TopologyBuilder now has a SetEventHubSpout method that takes in EventHubSpoutConfig to make easier to read data from Azure EventHubs.
  
  ```csharp
    topologyBuilder.SetEventHubSpout(
        "EventHubSpout",
        new EventHubSpoutConfig(
            ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"],
            ConfigurationManager.AppSettings["EventHubSharedAccessKey"],
            ConfigurationManager.AppSettings["EventHubNamespace"],
            ConfigurationManager.AppSettings["EventHubEntityPath"],
            partitionCount),
            partitionCount
        );
  ```
2. "SCPHost.exe.config" has now been deprecated, please move all your configurations into your App.config. 

SCPHost.exe, the container executable from SCP.Net thats runs your tasks will load your "Assembly.dll.config" as the default application configuration at execution time. 

The change is also applicable to ScpC.exe, the compiler executable that takes care of generating the spec file during compilation.
  
  ```xml
  <appSettings>
    <add key="EventHubNamespace" value="##TOBEFILLED##" />
    <add key="EventHubEntityPath" value="##TOBEFILLED##" />
    <add key="EventHubSharedAccessKeyName" value="##TOBEFILLED##" />
    <add key="EventHubSharedAccessKey" value="##TOBEFILLED##" />
  </appSettings>
  ```
3. Introduced StormConfig in TopologyBuilder that makes it easy to provide common used Storm configuration for Spouts, Bolts or Topologies.
  
  ```csharp
  //HOW-TO set config for Bolts:
  var boltConfig = new StormConfig();
  boltConfig.Set("topology.tick.tuple.freq.secs", "5");

  topologyBuilder.SetBolt(
      typeof(HBaseBolt).Name,
      HBaseBolt.Get,
      new Dictionary<string, List<string>>(),
      1,
      true).
      globalGrouping(typeof(VehicleRecordGeneratorSpout).Name).
      addConfigurations(boltConfig);
                
  //HOW-TO set config for Topologies:
  var topologyConfig = new StormConfig();
  //Set the number of workers
  topologyConfig.setNumWorkers(4);
  //Set max pending msgs
  topologyConfig.setMaxSpoutPending(1024);
  //Set worker process options
  topologyConfig.setWorkerChildOps("-Xmx1024m");

  //Set the topology config
  topologyBuilder.SetTopologyConfig(topologyConfig);
  ```
4. The JavaComponentConstructor can now handle complex Java types and is now easier to use.
  
  ```csharp
  // HOW-TO set parameters to initialize the constructor of Java Spout/Bolt
  JavaComponentConstructor generatorConfig = new JavaComponentConstructor(
      "microsoft.scp.example.HybridTopology.GeneratorConfig",
      new List<Tuple<string, object>>()
      {
          Tuple.Create<string, object>(JavaComponentConstructor.JAVA_PRIMITIVE_TYPE_INT, 100),
          Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, "test")
      });

  JavaComponentConstructor generator = new JavaComponentConstructor(
      "microsoft.scp.example.HybridTopology.Generator",
      new List<Tuple<string, object>>()
      {
          Tuple.Create<string, object>("microsoft.scp.example.HybridTopology.GeneratorConfig", generatorConfig)
      });

  topologyBuilder.SetJavaSpout(
      "generator",
      generator,
      1);
  ```
5. A ScpWebApiClient tool has been included in the nuget package under the tools directory.
It provides functionality to submit/activate/deactivate/rebalance/kill topologies easily using SCPAPI on HDInsight clusters.
You need to set required values inside the ScpWebApiClient.exe.config to use it. For example:
  
  ```xml
  <appSettings>
    <add key="SCPAPIURL" value="https://realtimeetl201503311303hbase.azurehdinsight.net/SCPAPI/"/>
    <add key="UseCredential" value="true"/>
    <add key="Username" value="##CLUSTER_USERNAME##"/>
    <add key="Password" value="##CLUSTER_PASSWORD##"/>
    <add key="OutputResponse" value="true"/>
  </appSettings>
  ```
### BUG FIXES:
1. Fixed the issue of the spec compilation failure (during generate-spec phase in build) if the project path contains spaces or project name contained dots.
2. ScpC.exe can now be used outside Visual Studio to automate spec and package generation.
3. Unimplemented methods are now hidden from the user.
