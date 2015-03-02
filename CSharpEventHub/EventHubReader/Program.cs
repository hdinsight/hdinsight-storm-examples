using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SCP;
using Microsoft.SCP.Topology;

namespace EventHubReader
{
    /// <summary>
    /// A hybrid C#/Java topology
    ///     The Java-based EventHubSpout reads from event hub
    ///     Data is then parsed by C# and written to Table Storage
    /// </summary>
    [Active(true)]
    class Program : TopologyDescriptor
    {
        static void Main(string[] args)
        {
        }

        /// <summary>
        /// Builds a topology that can be submitted to Storm on HDInsight
        /// </summary>
        /// <returns>A topology builder</returns>
        public ITopologyBuilder GetTopologyBuilder()
        {
            //The friendly name of this topology is 'EventHubReader'
            TopologyBuilder topologyBuilder = new TopologyBuilder("EventHubReader");

            //Get the partition count
            int partitionCount = Properties.Settings.Default.EventHubPartitionCount;
            //Create the constructor for the Java spout
            JavaComponentConstructor constructor = JavaComponentConstructor.CreateFromClojureExpr(
                String.Format(@"(com.microsoft.eventhubs.spout.EventHubSpout. (com.microsoft.eventhubs.spout.EventHubSpoutConfig. " +
                    @"""{0}"" ""{1}"" ""{2}"" ""{3}"" {4} ""{5}""))",
                    Properties.Settings.Default.EventHubPolicyName,
                    Properties.Settings.Default.EventHubPolicyKey,
                    Properties.Settings.Default.EventHubNamespace,
                    Properties.Settings.Default.EventHubName,
                    partitionCount,
                    "")); //zookeeper connection string - leave empty

            //Set the spout to use the JavaComponentConstructor
            topologyBuilder.SetJavaSpout(
                "EventHubSpout",  //Friendly name of this component
                constructor,      //Pass in the Java constructor
                partitionCount);  //Parallelism hint - partition count

            // Use a JSON Serializer to serialize data from the Java Spout into a JSON string
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

            //Set the C# bolt that consumes data from the spout
            topologyBuilder.SetBolt(
                "Bolt",                                              //Friendly name of this component
                Bolt.Get,
                new Dictionary<string, List<string>>(),
                partitionCount).                                     //Parallelisim hint - partition count
                DeclareCustomizedJavaSerializer(javaSerializerInfo). //Use the serializer when sending to the bolt
                shuffleGrouping("EventHubSpout");                    //Consume data from the 'EventHubSpout' component

            return topologyBuilder;
        }
    }
}

