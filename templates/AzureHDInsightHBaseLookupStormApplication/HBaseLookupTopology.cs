using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureHDInsightHBaseLookupStormApplication
{
    /// <summary>
    /// A topology example of reading data from spout and writing into HBase
    /// Uncomment Active attribute to deploy this topology and comment out Active attribute of any other topologies in this project.
    /// </summary>
    [Active(true)]
    class HBaseLookupTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            var topologyBuilder = new TopologyBuilder(typeof(HBaseLookupTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            topologyBuilder.SetSpout(
                typeof(VehicleRecordGeneratorSpoutForHBase).Name, //Set task name
                VehicleRecordGeneratorSpoutForHBase.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>() { 
                    { Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpoutForHBase.OutputFields } },
                1, //Set number of tasks
                true //Set enableAck
                );

            topologyBuilder.SetBolt(
                typeof(HBaseLookupBolt).Name, //Set task name
                HBaseLookupBolt.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>() { 
                    { Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpoutForHBase.OutputFields } },
                1, //Set number of tasks
                true //Set enableAck
                ).
                globalGrouping(typeof(VehicleRecordGeneratorSpoutForHBase).Name);

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