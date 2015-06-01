using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;

namespace Scp.App.HelloWorldTx
{
    public class HelloWorldTx
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
                    System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorldTx-Generator");

                    // SCPRuntime.Initialize() should be called before SCPRuntime.LaunchPlugin
                    SCPRuntime.Initialize();
                    SCPRuntime.LaunchPlugin(new newSCPPlugin(Generator.Get));
                }
                else if ("partial-count".Equals(compName))
                {
                    System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorldTx-PartialCount");
                    SCPRuntime.Initialize();
                    SCPRuntime.LaunchPlugin(new newSCPPlugin(PartialCount.Get));
                }
                else if ("count-sum".Equals(compName))
                {
                    System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorldTx-CountSum");
                    SCPRuntime.Initialize();
                    SCPRuntime.LaunchPlugin(new newSCPPlugin(CountSum.Get));
                }
                else
                {
                    throw new Exception(string.Format("unexpected compName: {0}", compName));
                }
            }
            else// if there is no args, run local test.
            {
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorldTx-LocalTest");
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
    }
}
