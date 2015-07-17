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
            StormConfig conf = new StormConfig();
            conf.setNumWorkers(1);
            conf.setWorkerChildOps("-Xmx1024m");
            conf.Set("topology.kryo.register", "[\"[B\"]");
            topologyBuilder.SetTopologyConfig(conf);

            return topologyBuilder;
        }
    }
}
