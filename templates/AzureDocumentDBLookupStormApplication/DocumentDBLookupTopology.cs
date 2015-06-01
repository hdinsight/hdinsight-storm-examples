using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System;
using System.Collections.Generic;

namespace AzureDocumentDBLookupStormApplication
{
    [Active(true)]
    public class DocumentDBLookupTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            var topologyBuilder = new TopologyBuilder(typeof(DocumentDBLookupTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            topologyBuilder.SetSpout(
                typeof(VehicleRecordGeneratorSpoutForDocumentDB).Name, //Set task name
                VehicleRecordGeneratorSpoutForDocumentDB.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>() { { Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpoutForDocumentDB.OutputFields } },
                1,
                true);

            topologyBuilder.SetBolt(
                typeof(DocumentDbLookupBolt).Name, //Set task name
                DocumentDbLookupBolt.Get, //Set task constructor delegate
                //Set the output field names - As DocumentDb return a JSON object, we will be expecting only 1 field
                new Dictionary<string, List<string>>() { { Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpoutForDocumentDB.OutputFields } },
                1,
                true).
                globalGrouping(typeof(VehicleRecordGeneratorSpoutForDocumentDB).Name);

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

