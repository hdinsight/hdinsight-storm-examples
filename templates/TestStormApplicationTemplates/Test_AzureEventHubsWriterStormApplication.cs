using AzureEventHubsWriterStormApplication;
using Microsoft.SCP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestStormApplicationTemplates
{
    public class Test_AzureEventHubsWriterStormApplication
    {
        public const string CLASS_NAME = "Test_AzureEventHubsWriterStormApplication";
        public static void Generator()
        {
            Console.WriteLine(CLASS_NAME + "generator");

            File.WriteAllText(CLASS_NAME + "generator.out", "");
            for (int i = 0; i < 1000; i++)
            {
                File.AppendAllText(CLASS_NAME + "generator.out",
                    JsonConvert.SerializeObject(IISLogGeneratorSpout.GetRandomIISLog()) + 
                    Environment.NewLine);
            }
        }
        
        public static void Run()
        {
            Console.WriteLine(CLASS_NAME + "run");

            Dictionary<string, Object> parms = new Dictionary<string, object>();

            {
                LocalContext spoutCtx = LocalContext.Get();
                var spout = IISLogGeneratorSpout.Get(spoutCtx, parms);

                for (int i = 0; i < 10; i++)
                {
                    spout.NextTuple(parms);
                }
                spoutCtx.WriteMsgQueueToFile(CLASS_NAME + "spout.out");
            }

            parms.Add("inputSchema", IISLogGeneratorSpout.OutputFieldTypes);

            {
                LocalContext boltCtx = LocalContext.Get();
                var bolt = LoggerBolt.Get(boltCtx, parms);
                boltCtx.ReadFromFileToMsgQueue(CLASS_NAME + "spout.out");
                List<SCPTuple> batch = boltCtx.RecvFromMsgQueue();
                foreach (SCPTuple tuple in batch)
                {
                    bolt.Execute(tuple);
                }
                boltCtx.WriteMsgQueueToFile(CLASS_NAME + "logger.out");
            }

            {
                LocalContext boltCtx = LocalContext.Get();
                var bolt = EventHubBolt.Get(boltCtx, parms);
                boltCtx.ReadFromFileToMsgQueue(CLASS_NAME + "spout.out");
                List<SCPTuple> batch = boltCtx.RecvFromMsgQueue();
                foreach (SCPTuple tuple in batch)
                {
                    bolt.Execute(tuple);
                }
                boltCtx.WriteMsgQueueToFile(CLASS_NAME + "bolt.out");
            }
        }
    }
}