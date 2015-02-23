# SCP.Net
SCP stands for Stream Computing Platform.

SCP.Net provides .Net C# programmability against Apache Storm on Azure HDInsight clusters.

## Getting Started
SCP.Net is now available as a nuget package: [Microsoft.SCP.Net.SDK](http://www.nuget.org/packages/Microsoft.SCP.Net.SDK/)

When you create a new Storm Application through Visual Studio using [HDInsight Tools](http://azure.microsoft.com/en-us/documentation/articles/hdinsight-hadoop-visual-studio-tools-get-started), SCP.Net is autommatically added into your project.

Also refer the MSDN article on [Developing Storm C# Topology in VS](http://azure.microsoft.com/en-us/documentation/articles/hdinsight-storm-develop-csharp-visual-studio-topology/)

There are some additional SCP.Net examples available at: [SCP.Net Examples](https://github.com/weedqian/hdinsight-storm-examples)

## SCP.Net Applications & Configuration
There are multiple ways of creating applications in SCP.Net. 

### Class Library (Default)
The default way in Visual Studio is to create a "Class Library" Storm Project, which uses the SCPHost.exe as the execution container to run one's spout or bolt tasks.
* Pros
  * One does not need to bother about writing invocation logic as SCPHost.exe takes care of it
  * One can leverage the TopologyDescriptor & Topology Builder interfaces for automatic topology spec generation
* Cons
  * One has to put their application configuration settings inside SCPHost.exe.config as that is the default execution container
  * You may need to add an empty App.Config file with name ```SCPHost.exe.config``` manually into your project if it was not added or you mistakenly removed it

Reference example: [Real Time ETL Topology](realtimeetl/README.md)
* The ```realtimeetl\EventHubAggregatorToHBaseTopology\SCPHost.exe.config``` contains all the application settings
* Refer to ```realtimeetl\EventHubAggregatorToHBaseTopology\Common\AppConfig.cs``` for an alternative way to load any App.Config (by default it loads ```SCPHost.exe.config```).
  * One can also choose to directly use ```ConfigurationManager.AppSettings["YOUR_SETTING_NAME"];```

### Executable
Alternatively, one can also create a "Executable" type of a project and can handle the invocation themselves. 
This requires one to implement a "static main" method and handle input arguments.

* Pros
  * As its your application that runs now, one can use the App.Config of the project to set application configurations
  * One can also pass in additional input parameters to the executable in addition to plugin information
* Cons
  * You need to implement the "static main" method that will take care of the invocation of the plugins
```csharp
/// <summary>
/// Start this process as a "Generator/Splitter/Counter", by specify the component name in commandline
/// If there is no args, run local test. 
/// </summary>
/// <param name="args"></param>
static void Main(string[] args)
{
    if (args.Count() > 0)
    {
        string compName = args[0];

        if ("generator".Equals(compName))
        {
            // Set the environment variable "microsoft.scp.logPrefix" to change the name of log file
            System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorld-Generator");

            // SCPRuntime.Initialize() should be called before SCPRuntime.LaunchPlugin
            SCPRuntime.Initialize();
            SCPRuntime.LaunchPlugin(new newSCPPlugin(Generator.Get));
        }
        else if ("splitter".Equals(compName))
        {
            System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorld-Splitter");
            SCPRuntime.Initialize();
            SCPRuntime.LaunchPlugin(new newSCPPlugin(Splitter.Get));
        }
        else if ("counter".Equals(compName))
        {
            System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorld-Counter");
            SCPRuntime.Initialize();
            SCPRuntime.LaunchPlugin(new newSCPPlugin(Counter.Get));
        }
        else
        {
            throw new Exception(string.Format("unexpected compName: {0}", compName));
        }
    }
    else// if there is no args, run local test.
    {
        System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorld-LocalTest");
        SCPRuntime.Initialize();

        // Make sure SCPRuntime is initialized as Local mode
        if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
        {
            throw new Exception(string.Format("unexpected pluginType: {0}", Context.pluginType));
        }
        LocalTest localTest = new LocalTest();
        localTest.RunTestCase();
    }
}
```


## Storm Configuration
This section describes how you can pass Apache Storm specific settings from your SCP.Net C# program.

### Topology Builder Interface
If you are using the "Class Library" type of applicaiton, you can add these settings directly into Topology Builder.

```csharp
TopologyBuilder topologyBuilder = new TopologyBuilder(this.GetType().Name);

//Assuming a 4 'Large' node cluster we will use half of the worker slots for this topology
//The default JVM heap size for workers is 768m, we also increase that to 1024m
//That helps the java spout have additional heap size at disposal.
topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
{
    {"topology.workers","8"},
    {"topology.max.spout.pending","1000"},
    {"topology.worker.childopts",@"""-Xmx1024m"""}
});
```

### Spec File (Clojure)
If you are using a spec file to describe your topology, you can specify a ":config" section at the bottom with the Storm specific configurations.
```clojure
:config
  {
    "topology.kryo.register" ["[B"]
    "topology.worker.childopts" "-Xmx1024m"
  }
```


## Hybrid Mode (Java & C#)
SCP.Net supports mixed (hybrid) mode topologies wherein you can have a Java Spout with C# bolt or C# spout with Java bolt.

### Topology Builder
One should use the topology builder provided in SCP.Net to create their topologies. 
This option automatically takes care of generating the ```.spec``` file for you which is used during topology submission.

#### Simple Java Constuctors
```csharp
// Demo how to set parameters to initialize the constructor of Java Spout/Bolt
List<object> constructorParams = new List<object>() { 100, "test", null };
List<string> paramTypes = new List<string>() { "int", "java.lang.String", "java.lang.String" };

JavaComponentConstructor constructor = new JavaComponentConstructor("microsoft.scp.example.HybridTopology.Generator", constructorParams, paramTypes);
topologyBuilder.SetJavaSpout(
    "generator",
    constructor,
    1);
```

#### Advanced Java Constructors
If you have Java Constructors that take nested Java Constructors, you can directly inject some clojure expressions into topology builder.
```csharp
//We will use CreateFromClojureExpr method as we wish to pass in a complex Java object 
//The EventHubSpout takes a EventHubSpoutConfig that we will create using clojure
//NOTE: We need to escape the quotes for strings that need to be passed to clojure
JavaComponentConstructor constructor = 
  JavaComponentConstructor.CreateFromClojureExpr(
  String.Format(@"(com.microsoft.eventhubs.spout.EventHubSpout. (com.microsoft.eventhubs.spout.EventHubSpoutConfig. " +
  @"""{0}"" ""{1}"" ""{2}"" ""{3}"" {4} """"))",
  appConfig.EventHubUsername, appConfig.EventHubPassword, 
  appConfig.EventHubNamespace, appConfig.EventHubEntityPath, 
  appConfig.EventHubPartitions));

topologyBuilder.SetJavaSpout(
  "EventHubSpout",
  constructor,
  appConfig.EventHubPartitions);
```

### Utilizing the automatic Java serialization & deserialization
SCP.Net provides you automatic Java to C# and vice-versa serliazation and deserialization using JSON.
To use this option one needs to use the provided JSON seriliazer and deserialer classes and set them in Topology Builder.

NOTE: 
* You do NOT need this option if you are doing a Java to Java or C# to C# data transfer.
* As that data is just raw bytes that is transfered, you can also choose to implement your own serializers or deserializers.

Both of the options below are covered in the realtimeetl example: ```realtimeetl/EventHubAggregatorToHBaseTopology/EventHubAggregatorToHBaseTopology.cs```

#### Java -> C#
This section shows how to setup your customized SerDe for Java to C#.
* C# Spout Constructor: We need to configure C# spout constructor to serialize the objects as JSON
```csharp
//This statement is used for Hybrid scenarios where you will add a customized serializer in C# spout 
//and a customized deserializer in your java bolt
this.context.DeclareCustomizedSerializer(new CustomizedInteropJSONSerializer());
```
* Topology Builder Section: We need to configure the topology builder so that the Java bolt receives objects Deserialized from JSON
```csharp
// Set a customized JSON Deserializer to deserialize a C# object (emitted by C# Spout) into JSON string for Java to Deserialize
// Here, fullname of the Java JSON Deserializer class is required followed by the Java types for each of the fields
List<string> javaDeserializerInfo = 
    new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer", "java.lang.String" };

topologyBuilder.SetSpout(
        typeof(EventGenerator).Name,
        EventGenerator.Get,
        new Dictionary<string, List<string>>()
        {
           {Constants.DEFAULT_STREAM_ID, new List<string>(){"Event"}}
        },
        appConfig.EventHubPartitions,
        true
    ).
    DeclareCustomizedJavaDeserializer(javaDeserializerInfo);
```
* 

#### C# -> Java
This section shows how to setup your customized SerDe for C# to Java.

* Topology Builder Section: We need to configure the topology so that the C# bolt receives objects serialized from Java to JSON
```csharp
// Set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
// Here, fullname of the Java JSON Serializer class is required
List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

topologyBuilder.SetBolt(
        typeof(EventAggregator).Name,
        EventAggregator.Get,
        new Dictionary<string, List<string>>()
        {
            {Constants.DEFAULT_STREAM_ID, new List<string>(){ "AggregationTimestamp", "PrimaryKey", "SecondaryKey", "AggregatedValue" } }
        },
        appConfig.EventHubPartitions,
        true
    ).
    DeclareCustomizedJavaSerializer(javaSerializerInfo).
    shuffleGrouping("EventHubSpout");
```
* C# Bolt Constructor: We also need to setup the constructor of C# bolt to deserialize the JSON serialized objects
```csharp
this.context.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());
```
