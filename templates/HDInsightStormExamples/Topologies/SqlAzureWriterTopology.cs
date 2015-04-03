using HDInsightStormExamples.Bolts;
using HDInsightStormExamples.Spouts;
using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDInsightStormExamples.Topologies
{
    /// <summary>
    /// A topology example of reading data from spout and writing into Sql Azure
    /// Uncomment Active attribute to deploy this topology and comment out Active attribute of any other topologies in this project.
    /// </summary>
    //[Active(true)]
    class SqlAzureWriterTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            var topologyBuilder = new TopologyBuilder(typeof(SqlAzureWriterTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            //Component tasks expect output field names in TopologyBuilder
            topologyBuilder.SetSpout(
                typeof(IISLogGeneratorSpout).Name, //Set task name
                IISLogGeneratorSpout.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>() { {Constants.DEFAULT_STREAM_ID, IISLogGeneratorSpout.OutputFields} },
                1, //Set number of tasks
                true //Set enableAck
                );

            topologyBuilder.SetBolt(
                typeof(LoggerBolt).Name, //Set task name
                LoggerBolt.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>(), //Leave empty if the task has no outputSchema defined i.e. no outgoing tuples
                1, //Set number of tasks
                true //Set enableAck
                ).
                globalGrouping(typeof(IISLogGeneratorSpout).Name);

            topologyBuilder.SetBolt(
                typeof(SqlAzureBolt).Name, //Set task name
                SqlAzureBolt.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>(), //Leave empty if the task has no outputSchema defined i.e. no outgoing tuples
                1, //Set number of tasks
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