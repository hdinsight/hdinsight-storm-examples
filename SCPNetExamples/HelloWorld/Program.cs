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
    [Active(true)]
    class HelloWorld : TopologyDescriptor
    {
        /// <summary>
        /// Start this process as a "Generator/Splitter/Counter", by specify the component name in commandline
        /// If there is no args, run local test. 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                string compName = args[0];

                if ("generator".Equals(compName))
                {
                    // Set the environment variable "microsoft.scp.logPrefix" to change the name of log file
                    System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorld-Generator");

                    // SCPRuntime.Initialize() should be called before SCPRuntime.LaunchPlugin
                    SCPRuntime.Initialize();
                    SCPRuntime.LaunchPlugin(new newSCPPlugin(Generator.Get));
                }
                else if ("splitter".Equals(compName))
                {
                    System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorld-Splitter");
                    SCPRuntime.Initialize();
                    SCPRuntime.LaunchPlugin(new newSCPPlugin(Splitter.Get));
                }
                else if ("counter".Equals(compName))
                {
                    System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorld-Counter");
                    SCPRuntime.Initialize();
                    SCPRuntime.LaunchPlugin(new newSCPPlugin(Counter.Get));
                }
                else
                {
                    throw new Exception(string.Format("unexpected compName: {0}", compName));
                }
            }
            else// if there is no args, run local test.
            {
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorld-LocalTest");
                SCPRuntime.Initialize();

                // Make sure SCPRuntime is initialized as Local mode
                if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
                {
                    throw new Exception(string.Format("unexpected pluginType: {0}", Context.pluginType));
                }
                LocalTest localTest = new LocalTest();
                localTest.RunTestCase();
            }
        }

        public ITopologyBuilder GetTopologyBuilder()
        {
            string pluginName = string.Format("{0}.exe", GetType().Assembly.GetName().Name);

            TopologyBuilder topologyBuilder = new TopologyBuilder("HelloWorld");
            topologyBuilder.SetSpout(
                "generator",
                pluginName,
                new List<string>() { "generator" },
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"sentence"}}
                },
                1);

            topologyBuilder.SetBolt(
                "splitter",
                pluginName,
                new List<string>() { "splitter" },
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"word", "firstLetterOfWord"}}
                },
                1).shuffleGrouping("generator");

            topologyBuilder.SetBolt(
                "counter",
                pluginName,
                new List<string>() { "counter" },
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"word", "count"}}
                },
                1).fieldsGrouping("splitter", new List<int>() { 1 });

            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
            {
                {"topology.kryo.register","[\"[B\"]"}
            });

            return topologyBuilder;
        }
    }
}
