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
    /// TransactionalTopologyBuilder hybrid topology example with CSharp Spout and Java Bolt
    /// </summary>
    class HybridTopologyTx_csharpSpout_javaBolt : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            TransactionalTopologyBuilder topologyBuilder = new TransactionalTopologyBuilder("HybridTopologyTx_csharpSpout_javaBolt");

            // Demo how to set a customized JSON Deserializer to deserialize a JSON string into Java object (to send to a Java Bolt)
            // Here, fullname of the Java JSON Deserializer class and target deserialized class are required
            List<string> javaDeserializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer", "microsoft.scp.example.HybridTopology.Person" };

            topologyBuilder.SetSpout(
                "generator",
                TxGenerator.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"person"}}
                },
                1,
                null).DeclareCustomizedJavaDeserializer(javaDeserializerInfo);

            // Demo how to use clojure code (from a text file) to initialize the constructor of Java Spout/Bolt
            JavaComponentConstructor constructor = JavaComponentConstructor.CreateFromClojureFile("Clojure.txt");
            topologyBuilder.SetJavaBolt(
                "displayer",
                constructor,
                1).shuffleGrouping("generator");

            // Demo how to set topology config
            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.kryo.register","[\"[B\"]"}
            });

            return topologyBuilder;
        }
    }
}
