using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System.Configuration;

/// <summary>
/// This program shows the ability to create a SCP.NET topology consuming JAVA Spouts
/// For how to use SCP.NET, please refer to: http://go.microsoft.com/fwlink/?LinkID=525500&clcid=0x409
/// For more Storm samples, please refer to our GitHub repository: http://go.microsoft.com/fwlink/?LinkID=525495&clcid=0x409
/// </summary>

namespace EventCountHybridTopology
{
    /// <summary>
    /// TopologyBuilder hybrid topology example with Java Spout and CSharp Bolt
    /// This TopologyDescriptor is marked as Active
    /// </summary>
    [Active(true)]
    public class EventCountHybridTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            var enableAck = bool.Parse(ConfigurationManager.AppSettings["EnableAck"]);

            TopologyBuilder topologyBuilder = 
                new TopologyBuilder(typeof(EventCountHybridTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            var eventHubPartitions = int.Parse(ConfigurationManager.AppSettings["EventHubPartitions"]);

            var eventHubSpoutConfig = new JavaComponentConstructor(
                "com.microsoft.eventhubs.spout.EventHubSpoutConfig",
                new List<Tuple<string, object>>() 
                { 
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"]),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, ConfigurationManager.AppSettings["EventHubSharedAccessKey"]),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, ConfigurationManager.AppSettings["EventHubNamespace"]),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, ConfigurationManager.AppSettings["EventHubEntityPath"]),
                    Tuple.Create<string, object>("int", eventHubPartitions),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, ""),
                    Tuple.Create<string, object>("int", 10),
                    Tuple.Create<string, object>("int", 1024),
                    Tuple.Create<string, object>("int", 1024*eventHubPartitions),
                    Tuple.Create<string, object>("long", 0),
                }
               );

            var eventHubSpout = new JavaComponentConstructor(
                "com.microsoft.eventhubs.spout.EventHubSpout",
                new List<Tuple<string, object>>() 
                { 
                    Tuple.Create<string, object>("com.microsoft.eventhubs.spout.EventHubSpoutConfig", eventHubSpoutConfig)
                }
               );

            topologyBuilder.SetJavaSpout("com.microsoft.eventhubs.spout.EventHubSpout", eventHubSpout, eventHubPartitions);

            // Set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
            // Here, full name of the Java JSON Serializer class is required
            List<string> javaSerializerInfo = new List<string>() { 
                "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

            var boltConfig = new StormConfig();
            boltConfig.Set("topology.tick.tuple.freq.secs", "1");

            topologyBuilder.SetBolt(
                    typeof(PartialCountBolt).Name,
                    PartialCountBolt.Get,
                    new Dictionary<string, List<string>>()
                    {
                        {Constants.DEFAULT_STREAM_ID, new List<string>(){ "partialCount" } }
                    },
                    eventHubPartitions,
                    enableAck
                ).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("com.microsoft.eventhubs.spout.EventHubSpout").
                addConfigurations(boltConfig);

            topologyBuilder.SetBolt(
                typeof(DBGlobalCountBolt).Name,
                DBGlobalCountBolt.Get,
                new Dictionary<string, List<string>>(),
                1,
                enableAck).
                globalGrouping(typeof(PartialCountBolt).Name).
                addConfigurations(boltConfig);

            var topologyConfig = new StormConfig();
            topologyConfig.setNumWorkers(eventHubPartitions);
            if (enableAck)
            {
                topologyConfig.setNumAckers(eventHubPartitions);
            }
            else
            {
                topologyConfig.setNumAckers(0);
            }
            topologyConfig.setWorkerChildOps("-Xmx1g");
            topologyConfig.setMaxSpoutPending((1024*1024)/100);

            topologyBuilder.SetTopologyConfig(topologyConfig);
            return topologyBuilder;
        }
    }
}
