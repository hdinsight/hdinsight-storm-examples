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
    /// TopologyBuilder hybrid topology example with Java Spout and CSharp Bolt
    /// This TopologyDescriptor is marked as Active
    /// </summary>
    [Active(true)]
    class HybridTopology_javaSpout_csharpBolt : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            TopologyBuilder topologyBuilder = new TopologyBuilder("HybridTopology_javaSpout_csharpBolt");

            // Demo how to set parameters to initialize the constructor of Java Spout/Bolt
            List<object> constructorParams = new List<object>() { 100, "test", null };
            List<string> paramTypes = new List<string>() { "int", "java.lang.String", "java.lang.String" };

            JavaComponentConstructor constructor = new JavaComponentConstructor("microsoft.scp.example.HybridTopology.Generator", constructorParams, paramTypes);
            topologyBuilder.SetJavaSpout(
                "generator",
                constructor,
                1);

            // Demo how to set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
            // Here, fullname of the Java JSON Serializer class is required
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer"};

            topologyBuilder.SetBolt(
                "displayer",
                Displayer.Get,
                new Dictionary<string, List<string>>(),
                1).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("generator");

            // Demo how to set topology config
            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.kryo.register","[\"[B\"]"}
            });

            return topologyBuilder;
        }
    }

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
            List<string> javaDeserializerInfo = new List<string>()
                { "microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer", "microsoft.scp.example.HybridTopology.Person"};

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
            List<object> constructorParams = new List<object>() { 100, "test", string.Empty };
            List<string> paramTypes = new List<string>() { "int", "java.lang.String", "java.lang.String" };

            JavaComponentConstructor constructor = new JavaComponentConstructor("microsoft.scp.example.HybridTopology.Displayer", constructorParams, paramTypes);
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

    /// <summary>
    /// TransactionalTopologyBuilder hybrid topology example with Java Spout and CSharp Bolt
    /// </summary>
    class HybridTopologyTx_javaSpout_csharpBolt : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            TransactionalTopologyBuilder topologyBuilder = new TransactionalTopologyBuilder("HybridTopologyTx_javaSpout_csharpBolt");

            // Demo how to use clojure code (in string) to initialize the constructor of Java Spout/Bolt
            JavaComponentConstructor constructor = JavaComponentConstructor.CreateFromClojureExpr("(microsoft.scp.example.HybridTopology.TxGenerator. 100 \"test\" nil)");
            topologyBuilder.SetJavaSpout(
                "generator",
                constructor,
                1);

            // Demo how to set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
            // Here, fullname of the Java JSON Serializer class is required
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };
            topologyBuilder.SetBolt(
                "displayer",
                SCPTxBolt.SCP_TX_COMMIT_BOLT,
                TxDisplayer.Get,
                new Dictionary<string, List<string>>(),
                1).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("generator");

            // Demo how to set topology config
            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.kryo.register","[\"[B\"]"}
            });

            return topologyBuilder;
        }
    }

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
