using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SCP;
using Microsoft.SCP.Topology;

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
        AppConfig appConfig;
        public ITopologyBuilder GetTopologyBuilder()
        {
            appConfig = new AppConfig();

            TopologyBuilder topologyBuilder = new TopologyBuilder(typeof(EventCountHybridTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            JavaComponentConstructor constructor =
                new JavaComponentConstructor("com.microsoft.eventhubs.spout.EventHubSpout",
                    new List<object>() { 
                                    appConfig.EventHubUsername, 
                                    appConfig.EventHubPassword,
                                    appConfig.EventHubNamespace, 
                                    appConfig.EventHubEntityPath,
                                    appConfig.EventHubPartitions},
                    new List<string>() { 
                                    "java.lang.String",
                                    "java.lang.String",
                                    "java.lang.String",
                                    "java.lang.String",
                                    "int"}
                );

            //For more advanced scenarios where you may wish to pass complex java objects
            //You can use the CreateFromClojureExpr method to pass in a clojure expression
            /*
            JavaComponentConstructor constructor =
                JavaComponentConstructor.CreateFromClojureExpr(
                String.Format(@"(com.microsoft.eventhubs.spout.EventHubSpout. (com.microsoft.eventhubs.spout.EventHubSpoutConfig. " +
                @"""{0}"" ""{1}"" ""{2}"" ""{3}"" {4} """"))",
                appConfig.EventHubUsername, appConfig.EventHubPassword,
                appConfig.EventHubNamespace, appConfig.EventHubEntityPath,
                appConfig.EventHubPartitions));
            */

            topologyBuilder.SetJavaSpout(
                "com.microsoft.eventhubs.spout.EventHubSpout",
                constructor,
                appConfig.EventHubPartitions);

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
                    appConfig.EventHubPartitions,
                    true
                ).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("com.microsoft.eventhubs.spout.EventHubSpout");

            topologyBuilder.SetBolt(
                typeof(DBGlobalCountBolt).Name,
                DBGlobalCountBolt.Get,
                new Dictionary<string, List<string>>(),
                1).
                globalGrouping(typeof(PartialCountBolt).Name);

            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.workers", appConfig.EventHubPartitions.ToString()},
                {"topology.max.spout.pending", "512"}
            });

            return topologyBuilder;
        }
    }
}
