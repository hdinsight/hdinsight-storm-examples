using EventHubAggregatorToHBaseTopology.Bolts;
using EventHubAggregatorToHBaseTopology.Common;
using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventHubAggregatorToHBaseTopology
{
    //IMPORTANT NOTE: All the logs from SCP.Net go into %STORM_HOME%\logs\scp directory on the respective worker nodes where the task is running
    //Each log from Storm & SCP.Net is also written into a "hadoopservicelog" table in your storage account
    //You can increase the readability of your logs by adding identifiers in your log messages

    /// <summary>
    /// A hybrid topology that generates a event in C# and uses Java bolt to write into EventHub
    /// </summary>
    class EventHubSenderHybridTopology : TopologyDescriptor
    {
        AppConfig appConfig;
        public ITopologyBuilder GetTopologyBuilder()
        {
            appConfig = new AppConfig();

            TopologyBuilder topologyBuilder = new TopologyBuilder(this.GetType().Name);

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

            //We will use CreateFromClojureExpr method as we wish to pass in a complex Java object 
            //The EventHubBolt takes a EventHubBoltConfig that we will create using clojure
            //NOTE: We need to escape the quotes for strings that need to be passes to clojure
            JavaComponentConstructor constructor =
                JavaComponentConstructor.CreateFromClojureExpr(
                String.Format(@"(com.microsoft.eventhubs.bolt.EventHubBolt. (com.microsoft.eventhubs.bolt.EventHubBoltConfig. " +
                @"""{0}"" ""{1}"" ""{2}"" ""{3}"" ""{4}"" {5}))",
                appConfig.EventHubUsername, appConfig.EventHubPassword,
                appConfig.EventHubNamespace, appConfig.EventHubFqnAddress,
                appConfig.EventHubEntityPath, "true"));

            topologyBuilder.SetJavaBolt(
                    "EventHubBolt",
                    constructor,
                    appConfig.EventHubPartitions
                ). 
                shuffleGrouping(typeof(EventGenerator).Name);

            //Assuming a 4 'L' node cluster, we will have 16 worker slots available
            //We will half of those slots for this topology
            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.workers","8"},
                {"topology.max.spout.pending","16000"}
            });

            return topologyBuilder;
        }
    }

    /// <summary>
    /// This is an example of using a pure C# topology to generate and sending events to EventHub
    /// </summary>
    class EventHubSenderCSharpTopology : TopologyDescriptor
    {
        AppConfig appConfig;
        public ITopologyBuilder GetTopologyBuilder()
        {
            appConfig = new AppConfig();

            TopologyBuilder topologyBuilder = new TopologyBuilder(this.GetType().Name);

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
                    appConfig.EventHubPartitions
                );

            topologyBuilder.SetBolt(
                    typeof(EventHubWriter).Name,
                    EventHubWriter.Get,
                    new Dictionary<string, List<string>>(),
                    appConfig.EventHubPartitions
                ).
                shuffleGrouping(typeof(EventGenerator).Name);

            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.workers","8"},
                {"topology.max.spout.pending","16000"}
            });

            return topologyBuilder;
        }
    }

    /// <summary>
    /// This is our main topology that fetches the Events from EventHub 
    /// And aggregates them each minute on PrimaryKey/Secondary Key
    /// You can choose to deploy multiple of such topologies to calculate different dimensions of data
    /// Or add more bolts within same topology by switching PrimaryKey & SecondaryKey values
    /// </summary>
    [Active(true)]
    class EventHubAggregatorToHBaseHybridTopology : TopologyDescriptor
    {
        AppConfig appConfig;
        public ITopologyBuilder GetTopologyBuilder()
        {
            appConfig = new AppConfig();

            TopologyBuilder topologyBuilder = new TopologyBuilder(this.GetType().Name);

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

            //You can also setup a Ranker bolt to maintain top N records
            /*
            topologyBuilder.SetBolt(
                    typeof(EventRanker).Name,
                    EventRanker.Get,
                    new Dictionary<string, List<string>>()
                    {
                        {Constants.DEFAULT_STREAM_ID, new List<string>(){ "AggregationTimestamp", "PrimaryKey", "SecondaryKey", "AggregatedValue" } }
                    },
                    appConfig.EventHubPartitions / 2
                ).
                fieldsGrouping(typeof(EventAggregator).Name, new List<int>() { 0, 1, 2 });
            */

            topologyBuilder.SetBolt(
                typeof(EventHBaseWriter).Name,
                EventHBaseWriter.Get,
                new Dictionary<string, List<string>>(),
                appConfig.EventHubPartitions / 4).
                fieldsGrouping(typeof(EventAggregator).Name, new List<int>() { 0, 1, 2 });

            //Assuming a 4 'Large' node cluster we will use half of the worker slots for this topology
            //The default JVM heap size for workers is 768m, we also increase that to 1024m
            //That helps the java spout have additional heap size at disposal.
            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.workers","8"},
                {"topology.max.spout.pending","1000"},
                {"topology.worker.childopts",@"""-Xmx1024m"""}
            });

            return topologyBuilder;
        }
    }
}
