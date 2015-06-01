using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Topology;

namespace Scp.App.HelloWorld
{
    /// <summary>
    /// Implements the TopologyDescriptor interface to describe the topology in C#,
    /// and return a ITopologyBuilder instance. 
    /// This TopologyDescriptor is marked as Active
    /// </summary>
    [Active(true)]
    class HelloWorld : TopologyDescriptor
    {
        /// <summary>
        /// Use Topology Specification API to describe the topology
        /// </summary>
        /// <returns></returns>
        public ITopologyBuilder GetTopologyBuilder()
        {
            // Use TopologyBuilder to define a Non-Tx topology
            // And define each spouts/bolts one by one
            TopologyBuilder topologyBuilder = new TopologyBuilder("HelloWorld");

            // Set a User customized config (Generator.config) for the Generator
            topologyBuilder.SetSpout(
                "generator",
                Generator.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"sentence"}}
                },
                1,
                "Generator.config");

            topologyBuilder.SetBolt(
                "splitter",
                Splitter.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"word", "firstLetterOfWord"}}
                },
                1).shuffleGrouping("generator");

            // Use scp-field-group from Splitter to Counter, 
            // and specify the second field in the Output schema of Splitter (Input schema of Counter) as the field grouping target
            // by passing the index array [1] (index start from 0) 
            topologyBuilder.SetBolt(
                "counter",
                Counter.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"word", "count"}}
                },
                1).fieldsGrouping("splitter", new List<int>() {1});

            // Demo how to set topology config
            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.kryo.register","[\"[B\"]"}
            });

            return topologyBuilder;
        }
    }
}
