using HDInsightStormExamples.Bolts;
using HDInsightStormExamples.Spouts;
using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDInsightStormExamples.Topologies
{
    /// <summary>
    /// A topology example of reading data from event hub spout
    /// TODO: IMPORTANT NOTE: Make sure you include the EventHubSpout jar with this topology so that the classes are available at runtime.
    /// Uncomment Active attribute to deploy this topology and comment out Active attribute of any other topologies in this project.
    /// </summary>
    //[Active(true)]
    class EventHubReaderTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            var topologyBuilder = new TopologyBuilder(typeof(EventHubReaderTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            var EventHubNamespace = ConfigurationManager.AppSettings["EventHubNamespace"];
            if (String.IsNullOrWhiteSpace(EventHubNamespace))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubNamespace");
            }

            var EventHubEntityPath = ConfigurationManager.AppSettings["EventHubEntityPath"];
            if (String.IsNullOrWhiteSpace(EventHubEntityPath))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubEntityPath");
            }

            var EventHubSharedAccessKeyName = ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"];
            if (String.IsNullOrWhiteSpace(EventHubSharedAccessKeyName))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubSharedAccessKeyName");
            }

            var EventHubSharedAccessKey = ConfigurationManager.AppSettings["EventHubSharedAccessKey"];
            if (String.IsNullOrWhiteSpace(EventHubSharedAccessKey))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubSharedAccessKey");
            }

            var EventHubPartitions = ConfigurationManager.AppSettings["EventHubPartitions"];
            if (String.IsNullOrWhiteSpace(EventHubPartitions))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "EventHubPartitions");
            }

            var partitionCount = int.Parse(EventHubPartitions);

            //You can use the new SetEventHubSpout method by providing EventHubSpoutConfig which will automatically create Java code to instantiate this spout
            //TODO: This method will not work if you do not include EventHub jar during publishing or deployment of this topology
            topologyBuilder.SetEventHubSpout(
                "EventHubSpout", //Set task name
                new EventHubSpoutConfig(
                    EventHubSharedAccessKeyName,
                    EventHubSharedAccessKey,
                    EventHubNamespace,
                    EventHubEntityPath,
                    partitionCount),
                    partitionCount
                );

            //For a hybrid topology we need to declare a customize Java serializer that will serialize Java objects which will deseriailized in the bolt into C# objects
            topologyBuilder.SetBolt(
                typeof(LoggerBolt).Name, //Set task name
                LoggerBolt.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>(), //Leave empty if the task has no outputSchema defined i.e. no outgoing tuples
                1, //Set number of tasks
                true //Set enableAck
                ).
                DeclareCustomizedJavaSerializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" } ).
                globalGrouping("EventHubSpout");

            //Set the topology config
            var topologyConfig = new StormConfig();
            topologyConfig.setNumWorkers(4); //Set number of worker processes
            topologyConfig.setMaxSpoutPending(1024); //Set maximum pending tuples from spout
            topologyConfig.setWorkerChildOps("-Xmx1024m"); //Set Java Heap Size

            return topologyBuilder;
        }
    }
}