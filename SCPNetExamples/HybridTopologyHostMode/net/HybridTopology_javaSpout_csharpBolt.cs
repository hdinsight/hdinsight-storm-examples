using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Topology;

namespace Scp.App.HybridTopologyHostMode
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
            JavaComponentConstructor generatorConfig = new JavaComponentConstructor(
                "microsoft.scp.example.HybridTopology.GeneratorConfig",
                new List<Tuple<string, object>>()
                {
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_PRIMITIVE_TYPE_INT, 100),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, "test")
                });

            JavaComponentConstructor generator = new JavaComponentConstructor(
                "microsoft.scp.example.HybridTopology.Generator",
                new List<Tuple<string, object>>()
                {
                    Tuple.Create<string, object>("microsoft.scp.example.HybridTopology.GeneratorConfig", generatorConfig)
                });

            topologyBuilder.SetJavaSpout(
                "generator",
                generator,
                1);

            // Demo how to set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
            // Here, fullname of the Java JSON Serializer class is required
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

            topologyBuilder.SetBolt(
                "displayer",
                Displayer.Get,
                new Dictionary<string, List<string>>(),
                1).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("generator");

            // Demo how to set topology config
            StormConfig conf = new StormConfig();
            conf.setNumWorkers(1);
            conf.setWorkerChildOps("-Xmx1024m");
            conf.Set("topology.kryo.register", "[\"[B\"]");
            topologyBuilder.SetTopologyConfig(conf);

            return topologyBuilder;
        }
    }
}
