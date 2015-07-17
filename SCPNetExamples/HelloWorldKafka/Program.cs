using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;

namespace Scp.App.HelloWorld
{
    class HelloWorld
    {
        static void Main(string[] args)
        {
            string compName = args[0];

            if ("partial-count".Equals(compName))
            {
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorldKafka-PartialCount");
                SCPRuntime.Initialize();
                SCPRuntime.LaunchPlugin(new newSCPPlugin(PartialCount.Get));
            }
            else if ("count-sum".Equals(compName))
            {
                System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "HelloWorldKafka-CountSum");
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
