using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System;
using System.Collections.Generic;
using System.Configuration;

/// <summary>
/// This program shows the ability to create a SCP.NET topology consuming JAVA Spouts
/// For how to use SCP.NET, please refer to: http://go.microsoft.com/fwlink/?LinkID=525500&clcid=0x409
/// For more Storm samples, please refer to our GitHub repository: http://go.microsoft.com/fwlink/?LinkID=525495&clcid=0x409
/// </summary>

namespace SignalRWriterStormApplication
{
    /// <summary>
    /// TopologyBuilder hybrid topology example with Java Spout and CSharp Bolt
    /// This TopologyDescriptor is marked as Active
    /// </summary>
    [Active(true)]
    public class SignalRBroadcastTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            TopologyBuilder topologyBuilder = new TopologyBuilder(typeof(SignalRBroadcastTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            var eventHubPartitions = int.Parse(ConfigurationManager.AppSettings["EventHubPartitions"]);

            topologyBuilder.SetEventHubSpout(
                "com.microsoft.eventhubs.spout.EventHubSpout", 
                new EventHubSpoutConfig(
                    ConfigurationManager.AppSettings["EventHubUsername"],
                    ConfigurationManager.AppSettings["EventHubPassword"],
                    ConfigurationManager.AppSettings["EventHubNamespace"], 
                    ConfigurationManager.AppSettings["EventHubEntityPath"],
                    eventHubPartitions),
                eventHubPartitions);

            // Set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
            // Here, full name of the Java JSON Serializer class is required
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

            topologyBuilder.SetBolt(
                    typeof(PartialCountBolt).Name,
                    PartialCountBolt.Get,
                    new Dictionary<string, List<string>>()
                    {
                        {Constants.DEFAULT_STREAM_ID, new List<string>(){ "partialCount" } }
                    },
                    eventHubPartitions,
                    true
                ).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("com.microsoft.eventhubs.spout.EventHubSpout").
                addConfigurations(new Dictionary<string, string>()
                {
                    {"topology.tick.tuple.freq.secs", "1"}
                });

            topologyBuilder.SetBolt(
                typeof(GlobalCountBolt).Name,
                GlobalCountBolt.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){ "globalCount" } }
                },
                1).
                globalGrouping(typeof(PartialCountBolt).Name).
                addConfigurations(new Dictionary<string,string>()
                {
                    {"topology.tick.tuple.freq.secs", "1"}
                });

            topologyBuilder.SetBolt(
                typeof(SignalRBroadcastBolt).Name,
                SignalRBroadcastBolt.Get,
                new Dictionary<string, List<string>>(),
                1).
                globalGrouping(typeof(GlobalCountBolt).Name);

            var topologyConfig = new StormConfig();
            topologyConfig.setMaxSpoutPending(512);
            topologyConfig.setNumWorkers(eventHubPartitions);
            topologyBuilder.SetTopologyConfig(topologyConfig);
            return topologyBuilder;
        }
    }
}
