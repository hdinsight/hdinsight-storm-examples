using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Topology;

namespace Scp.App.HybridTopology
{
    /// <summary>
    /// TopologyBuilder hybrid topology example with CSharp Spout and Java Bolt
    /// </summary>
    class HybridTopology_csharpSpout_javaBolt : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            TopologyBuilder topologyBuilder = new TopologyBuilder("HybridTopology_csharpSpout_javaBolt");

            // Demo how to set a customized JSON Deserializer to deserialize a JSON string into Java object (to send to a Java Bolt)
            // Here, fullname of the Java JSON Deserializer class and target deserialized class are required
            List<string> javaDeserializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer", "microsoft.scp.example.HybridTopology.Person" };

            topologyBuilder.SetSpout(
                "generator",
                Generator.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"person"}}
                },
                1,
                null).DeclareCustomizedJavaDeserializer(javaDeserializerInfo);

            // Demo how to set parameters to initialize the constructor of Java Spout/Bolt
            JavaComponentConstructor constructor = new JavaComponentConstructor(
                "microsoft.scp.example.HybridTopology.Displayer",
                new List<Tuple<string, object>>()
                {
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_PRIMITIVE_TYPE_INT, 100),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, "test"),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, string.Empty)
                });

            topologyBuilder.SetJavaBolt(
                "displayer",
                constructor,
                1).shuffleGrouping("generator");

            // Demo how to set topology config
            StormConfig conf = new StormConfig();
            conf.setDebug(false);
            conf.setNumWorkers(1);
            conf.setStatsSampleRate(0.05);
            conf.setWorkerChildOps("-Xmx1024m");
            conf.Set("topology.kryo.register", "[\"[B\"]");
            topologyBuilder.SetTopologyConfig(conf);

            return topologyBuilder;
        }
    }
}
