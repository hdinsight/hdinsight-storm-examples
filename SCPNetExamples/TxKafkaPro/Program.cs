using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;

namespace Scp.App.TxKafkaPro
{
    public class TxKafkaPro
    {
        static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                string compName = args[0];

                if ("kafkaspout".Equals(compName))
                {
                    System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "TxKafkaPro-KafkaSpout");
                    SCPRuntime.Initialize();
                    SCPRuntime.LaunchPlugin(new newSCPPlugin(KafkaSpout.Get));
                }
                else if ("partial-count".Equals(compName))
                {
                    System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "TxKafkaPro-PartialCount");
                    SCPRuntime.Initialize();
                    SCPRuntime.LaunchPlugin(new newSCPPlugin(PartialCount.Get));
                }
                else if ("count-sum".Equals(compName))
                {
                    System.Environment.SetEnvironmentVariable("microsoft.scp.logPrefix", "TxKafkaPro-CountSum");
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
}
