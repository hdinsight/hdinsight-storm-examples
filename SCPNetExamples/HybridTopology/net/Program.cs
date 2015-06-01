using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;

namespace Scp.App.HybridTopology
{
    class HybridTopology
    {
        /// <summary>
        /// Start this process as a "Generator/Displayer/Tx-Generator/Tx-Displayer", by specify the component name in commandline
        /// If there is no args, run local test. 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string compName = args[0];

            if ("generator".Equals(compName))
            {
                // Set the environment variable "microsoft.scp.logPrefix" to change the name of log file
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HybridTopology-Generator");

                // SCPRuntime.Initialize() should be called before SCPRuntime.LaunchPlugin
                SCPRuntime.Initialize();
                SCPRuntime.LaunchPlugin(new newSCPPlugin(Generator.Get));
            }
            else if ("displayer".Equals(compName))
            {
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HybridTopology-Displayer");
                SCPRuntime.Initialize();
                SCPRuntime.LaunchPlugin(new newSCPPlugin(Displayer.Get));
            }
            else if ("tx-generator".Equals(compName))
            {
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HybridTopology-TxGenerator");
                SCPRuntime.Initialize();
                SCPRuntime.LaunchPlugin(new newSCPPlugin(TxGenerator.Get));
            }
            else if ("tx-displayer".Equals(compName))
            {
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HybridTopology-TxDisplayer");
                SCPRuntime.Initialize();
                SCPRuntime.LaunchPlugin(new newSCPPlugin(TxDisplayer.Get));
            }
            else
            {
                throw new Exception(string.Format("unexpected compName: {0}", compName));
            }
        }
    }
}
