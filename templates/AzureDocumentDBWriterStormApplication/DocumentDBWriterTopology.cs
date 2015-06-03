using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System;
using System.Collections.Generic;

namespace AzureDocumentDBWriterStormApplication
{
    [Active(true)]
    public class DocumentDBWriterTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            var topologyBuilder = new TopologyBuilder(typeof(DocumentDBWriterTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            topologyBuilder.SetSpout(
                typeof(VehicleRecordGeneratorSpoutForDocumentDB).Name, //Set task name
                VehicleRecordGeneratorSpoutForDocumentDB.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>() { 
                    { Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpoutForDocumentDB.OutputFields } 
                },
                1, //Set number of tasks
                true //Set enableAck
                );

            //Store the incoming Vehicle records in DocumentDb
            topologyBuilder.SetBolt(
                typeof(DocumentDbBolt).Name, //Set task name
                DocumentDbBolt.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>(), //Leave empty if the task has no outputSchema defined i.e. no outgoing tuples
                1, //Set number of tasks
                true //Set enableAck
                ).
                globalGrouping(typeof(VehicleRecordGeneratorSpoutForDocumentDB).Name); //Choose grouping

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

