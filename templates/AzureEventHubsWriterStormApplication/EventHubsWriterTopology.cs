using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace AzureEventHubsWriterStormApplication
{
    /// <summary>
    /// A topology example of reading data from spout and writing into Azure EventHub
    /// Uncomment Active attribute to deploy this topology and comment out Active attribute of any other topologies in this project.
    /// </summary>
    [Active(true)]
    public class EventHubsWriterTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            var topologyBuilder = new TopologyBuilder(typeof(EventHubsWriterTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            topologyBuilder.SetSpout(
                typeof(IISLogGeneratorSpout).Name, //Set task name
                IISLogGeneratorSpout.Get,
                new Dictionary<string, List<string>>() { {Constants.DEFAULT_STREAM_ID, IISLogGeneratorSpout.OutputFields} },
                1, //Set number of tasks
                true //Set enableAck
                );

            var EventHubPartitions = ConfigurationManager.AppSettings["EventHubPartitions"];
            if (String.IsNullOrWhiteSpace(EventHubPartitions))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubPartitions");
            }

            var partitionCount = int.Parse(EventHubPartitions);

            topologyBuilder.SetBolt(
                typeof(EventHubBolt).Name, //Set task name
                EventHubBolt.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>(), //Leave empty if the task has no outputSchema defined i.e. no outgoing tuples
                partitionCount, //Set number of tasks
                true //Set enableAck
                ).
                globalGrouping(typeof(IISLogGeneratorSpout).Name);

            //Set the topology config
            var topologyConfig = new StormConfig();
            topologyConfig.setNumWorkers(1); //Set number of worker processes
            topologyConfig.setMaxSpoutPending(512); //Set maximum pending tuples from spout
            topologyConfig.setWorkerChildOps("-Xmx768m"); //Set Java Heap Size

            topologyBuilder.SetTopologyConfig(topologyConfig);

            return topologyBuilder;
        }
    }
}