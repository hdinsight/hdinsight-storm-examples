using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Topology;

namespace Scp.App.HelloWorldHostModeMultiSpout
{
    /// <summary>
    /// Implements the TopologyDescriptor interface to describe the topology in C#,
    /// and return a ITopologyBuilder instance. 
    /// This TopologyDescriptor is marked as Active
    /// </summary>
    [Active(true)]
    class HelloWorldHostModeMultiSpout : TopologyDescriptor
    {
        /// <summary>
        /// Use Topology Specification API to describe the topology
        /// </summary>
        /// <returns></returns>
        public ITopologyBuilder GetTopologyBuilder()
        {
            // Use TopologyBuilder to define a Non-Tx topology
            // And define each spouts/bolts one by one
            TopologyBuilder topologyBuilder = new TopologyBuilder("HelloWorldHostModeMultiSpout");

            // Set a User customized config (SentenceGenerator.config) for the SentenceGenerator
            topologyBuilder.SetSpout(
                "SentenceGenerator",
                SentenceGenerator.Get,
                new Dictionary<string, List<string>>()
                {
                    {SentenceGenerator.STREAM_ID, new List<string>(){"sentence"}}
                },
                1,
                "SentenceGenerator.config");

            topologyBuilder.SetSpout(
                "PersonGenerator",
                PersonGenerator.Get,
                new Dictionary<string, List<string>>()
                            {
                                {PersonGenerator.STREAM_ID, new List<string>(){"person"}}
                            },
                1);

            topologyBuilder.SetBolt(
                  "displayer",
                Displayer.Get,
                new Dictionary<string, List<string>>(),
                1)
                .shuffleGrouping("SentenceGenerator", SentenceGenerator.STREAM_ID)
                .shuffleGrouping("PersonGenerator", PersonGenerator.STREAM_ID);


            // Demo how to set topology config
            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.kryo.register","[\"[B\"]"}
            });

            return topologyBuilder;
        }
    }
}
