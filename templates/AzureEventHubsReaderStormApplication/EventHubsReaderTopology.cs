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

namespace EventHubsReaderTopology
{
    /// <summary>
    /// TopologyBuilder hybrid topology example with Java Spout and CSharp Bolt
    /// This TopologyDescriptor is marked as Active
    /// </summary>
    [Active(true)]
    public class EventHubsReaderTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            var enableAck = true;
            //var enableAck = bool.Parse(ConfigurationManager.AppSettings["EnableAck"]);

            TopologyBuilder topologyBuilder = 
                new TopologyBuilder(typeof(EventHubsReaderTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            var eventHubPartitions = int.Parse(ConfigurationManager.AppSettings["EventHubPartitions"]);

            topologyBuilder.SetEventHubSpout(
                "com.microsoft.eventhubs.spout.EventHubSpout", 
                new EventHubSpoutConfig(
                    ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"],
                    ConfigurationManager.AppSettings["EventHubSharedAccessKey"],
                    ConfigurationManager.AppSettings["EventHubNamespace"], 
                    ConfigurationManager.AppSettings["EventHubEntityPath"],
                    eventHubPartitions),
                eventHubPartitions);

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
                typeof(GlobalCountBolt).Name,
                GlobalCountBolt.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){ "timestamp", "totalCount" } }
                },
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
