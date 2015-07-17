using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SCP;

namespace Scp.App.HelloWorldKafka
{
    class HelloWorldKafka
    {
        static void Main(string[] args)
        {
            string compName = args[0];

            if ("partial-count".Equals(compName))
            {
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorldTxKafka-PartialCount");
                SCPRuntime.Initialize();
                SCPRuntime.LaunchPlugin(new newSCPPlugin(PartialCount.Get));
            }
            else if ("count-sum".Equals(compName))
            {
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorldTxKafka-CountSum");
                SCPRuntime.Initialize();
                SCPRuntime.LaunchPlugin(new newSCPPlugin(CountSum.Get));
            }
            else
            {
                throw new Exception(string.Format("unexpected compName: {0}", compName));
            }
        }
    }
}
