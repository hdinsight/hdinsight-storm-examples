Microsoft.SCP.Net.SDK - README
==============================

GETTING STARTED:
----------------
* http://azure.microsoft.com/en-us/documentation/articles/hdinsight-storm-develop-csharp-visual-studio-topology/
* https://github.com/hdinsight/hdinsight-storm-examples

CHANGELOG 0.9.4.346:
--------------------
NEW FEATURES:

1. SCP.NET logs will be displayed in Storm worker log files now, you will be also able to see them from VS/Storm UI.
There are two log pipelines from SCP.NET process to Storm worker, one is for STDERR, and the other one is for STDOUT.
You can control the log level of each pipeline by changing the threshold of ConsoleAppenderError and ConsoleAppender, 
which is defined in Microsoft.SCP.Net.SDK\sdk\Microsoft.SCPNet.dll.config.

    <appender name="ConsoleAppenderError" type="log4net.Appender.ConsoleAppender">
      <threshold value="ERROR"/>
      <target value="Console.Error" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="[%thread] %-5level %logger - %message%newline" />
      </layout>
    </appender>
    <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
      <threshold value="INFO"/>
      <target value="Console.Out" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="[%thread] %-5level %logger - %message%newline" />
      </layout>
    </appender>

2. SCP.net can now handle assembly binding redirections now as specified in App.config by creating a copy of the same as SCPHost.exe.config.
You can use it to solve version conflicts in your project dependencies.
For example, SCP.NET SDK package uses Newtonsoft.Json 6.0.4, but if one of your dependency requires Newtonsoft.Json 4.5.0 or above, 
you can add following assembly binding redirections to resolve the conflicts:

	<runtime>
	  <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
	    <dependentAssembly>
		  <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />
		  <bindingRedirect oldVersion="0.0.0.0-6.0.0.0" newVersion="6.0.0.0" />
	    </dependentAssembly>
	  </assemblyBinding>
	</runtime>

BUG FIXES:
1. Reference "Newtonsoft.Json" NuGet package explicitly in SCP.NET SDK NuGet package simplifying project dependencies.
2. SCPAPI returning HttpStatus 502 "Bad Gateway" error on TopologySubmit is now fixed in HDInsight Storm Cluster Version: 3.2.4.583 or above

KNOWN ISSUES:
1. SCPAPI on HDInsight can return HttpStatus 404 if your topology package size is larger than 30 MB. You can work around this problem by modifying its Web.config.
  a. Step 1: Remote Desktop into your cluster's headnode by enabling RDP and find out your active headnode via the Desktop short-cut: Hadoop Service Availability (you may have to refresh the page)
  b. Step 2: Go to C:\apps\dist\storm-<version>\SCPAPI\SCPAPI and open Web.config for editing
  c. Step 3: Update the value for httpRuntime maxRequestLength under system.Web from "51200" to "2097152"
    <system.web>
        <httpRuntime targetFramework="4.5" maxRequestLength="2097152" executionTimeout="300" />
  d. Step 4: Add a requestLimits maxAllowedContentLength under security in system.WebServer
    <system.webServer>
        <security>
            <requestFiltering>
                <requestLimits maxAllowedContentLength="2147483648" />
            </requestFiltering>
  e. The change is instant and you don't need to restart IIS. If you get HttpStatus 500 check the event logs if you did a mistake in configuration changes.

2. The new feature to display SCP.Net logs in Storm UI may show [ERROR] logs in [INFO] stream. This will be fixed in next HDInsight Storm cluster update.

3. Referencing "log4net" and "ZooKeeper.Net" NuGet packages explicitly causes runtime assembly load failures due to PublicKeyToken change of log4net assembly post version 1.2.11.0 [2.0.0].
The current "ZooKeeper.Net" NuGet package only can work with log4net 1.2.10.0 for now while SCP.Net requires log4net 1.2.11.0. 
Hence SCP.Net SDK contains its own compatible copies of "log4net.dll" and "ZooKeeperNet.dll" that have shipped without issues in previous versions:
log4net, Version=1.2.11.0, Culture=neutral, PublicKeyToken=669e0ddf0bb1aa2a
ZooKeeperNet, Version=7.7.0.0, Culture=neutral, PublicKeyToken=null

COMPATIBILITY:
Please use HDInsight Storm Cluster with version 3.2.4.583 or above with this SCP.Net latest NuGet package to get the most features and bug fixes.

OLDER CHANGELOGS
================

CHANGELOG 0.9.3.337: [HIDDEN]
--------------------
* This version is now hidden due to "Zookeeper.Net" and "log4net" NuGet incompatibility with newer versions of "log4net" (2.0.0 +).
Please upgrade to the latest publicly available version of Microsoft.SCP.Net.SDK to avoid any runtime assembly load failures.

CHANGELOG 0.9.4.283:
--------------------
NEW FEATURES:

1. TopologyBuilder now has a SetEventHubSpout method that takes in EventHubSpoutConfig to make easier to read data from Azure EventHubs.

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

2. "SCPHost.exe.config" has now been deprecated, please move all your configurations into your App.config. 
SCPHost.exe, the container executable from SCP.Net that runs your tasks will load your "Assembly.dll.config" as the default application configuration at execution time.
The change is also applicable to ScpC.exe, the compiler executable that takes care of generating the spec file during compilation.

  <appSettings>
    <add key="EventHubNamespace" value="##TOBEFILLED##" />
    <add key="EventHubEntityPath" value="##TOBEFILLED##" />
    <add key="EventHubSharedAccessKeyName" value="##TOBEFILLED##" />
    <add key="EventHubSharedAccessKey" value="##TOBEFILLED##" />
  </appSettings>

3. Introduced StormConfig in TopologyBuilder that makes it easy to provide common used Storm configuration for Spouts, Bolts or Topologies.

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


4. The JavaComponentConstructor can now handle complex Java types and is now easier to use.
    
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

5. A ScpWebApiClient tool has been included in the NuGet package under the tools directory.
It provides functionality to submit/activate/deactivate/rebalance/kill topologies easily using SCPAPI on HDInsight clusters.

Here is the usage of ScpWebApiClient.exe:
	Usage: ScpWebApiClient.exe <options> [-arg1 value1 -arg2 value2 ...] [general args]
	Usage of options:
	  ScpWebApiClient.exe submit -spec <spec file path> -packagefile <resource zip file path>
	  ScpWebApiClient.exe activate/deactivate -id <topology id>
	  ScpWebApiClient.exe activate/deactivate -name <topology name>
	  ScpWebApiClient.exe rebalance/kill -id <topology id> -waitSeconds <optional, wait seconds for rebalance/kill, 30s by default>
	  ScpWebApiClient.exe rebalance/kill -name <topology name> -waitSeconds <optional, wait seconds for rebalance/kill, 30s by default>

	General args:
	  -scpapiurl        Set the url of scpapi. For HDI cluster, it's "http://<HDI cluster address>/scpapi".
	  -username         Set the username for accessing scpapi. For HDI cluster, normally it's the "admin" account.
	  -password         Set the password for accessing scpapi to avoid using the password prompt when establishing connection to scpapi.
	  -slient           To disable web repsonse file logging.
	  -timeout          Set the timeout for HttpClient in miniutes, default value is 2.

	Examples:
	  ScpWebApiClient.exe submit -spec examples\HybridTopology\HybridTopology_javaSpout_csharpBolt.spec -packagefile HybridTopology.zip
	  ScpWebApiClient.exe deactivate -id HelloWorld-1-1427097167
	  ScpWebApiClient.exe kill -id HelloWorld-1-1427097167 -waitSeconds 10
  
For those general args, you can also set them in the config file, for example:
  <appSettings>
    <add key="SCPAPIURL" value="https://realtimeetl201503311303hbase.azurehdinsight.net/SCPAPI/"/>
    <add key="Username" value="##CLUSTER_USERNAME##"/>
    <add key="Password" value="##CLUSTER_PASSWORD##"/>
    <add key="OutputResponse" value="true"/>
  </appSettings>

BUG FIXES:
1. Fixed the issue of the spec compilation failure (during generate-spec phase in build) if the project path contains spaces or project name contained dots.
2. ScpC.exe can now be used outside Visual Studio to automate spec and package generation.
3. Unimplemented methods are now hidden from the user.
