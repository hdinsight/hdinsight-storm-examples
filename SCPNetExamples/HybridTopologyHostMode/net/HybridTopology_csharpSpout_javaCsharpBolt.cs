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
    /// TopologyBuilder hybrid topology example with a CSharp Spout, sending to both a C# bolt and a Java Bolt
    /// </summary>
    class HybridTopology_csharpSpout_javaCsharpBolt : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            TopologyBuilder topologyBuilder = new TopologyBuilder("HybridTopology_csharpSpout_javaCsharpBolt");

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

            // The java bolt "java_displayer" receives from the C# spout "generator"
            topologyBuilder.SetJavaBolt(
                "java_displayer",
                constructor,
                1).shuffleGrouping("generator");

            // Demo how to set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
            // Here, fullname of the Java JSON Serializer class is required
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

            // The C# bolt "csharp-displayer" receive from the C# spout "generator"
            topologyBuilder.SetBolt(
                "csharp-displayer",
                Displayer.Get,
                new Dictionary<string, List<string>>(),
                1).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("generator");

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
