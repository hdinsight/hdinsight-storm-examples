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
    internal class LocalTest
    {
        /// <summary>
        /// Here is a example to test the topology as a standalone console application.
        /// "LocalContext" is a local-mode SCP Context which is used to initialize a component,
        /// and each components communicate by using a plain text file.
        /// </summary>
        public void RunTestCase()
        {
            Dictionary<string, Object> emptyDictionary = new Dictionary<string, object>();
            Dictionary<string, Object> boltParms = new Dictionary<string, object>();

            StormTxAttempt txAttempt = new StormTxAttempt();
            txAttempt.TxId = 1;
            txAttempt.AttemptId = 1;
            boltParms[Constants.STORM_TX_ATTEMPT] = txAttempt;

            {
                // create a "Generator" object
                LocalContext generatorCtx = LocalContext.Get();
                Generator generator = Generator.Get(generatorCtx, emptyDictionary);

                // call nextTx()
                long seqId;
                generator.NextTx(out seqId, emptyDictionary);

                // write the messages in memory to files
                generatorCtx.WriteMsgQueueToFile("generator.txt");
            }

            {
                LocalContext partialCountCtx = LocalContext.Get();
                PartialCount partialCount = PartialCount.Get(partialCountCtx, boltParms);

                partialCountCtx.ReadFromFileToMsgQueue("generator.txt");
                List<SCPTuple> batch = partialCountCtx.RecvFromMsgQueue();
                foreach (SCPTuple tuple in batch)
                {
                    partialCount.Execute(tuple);
                }
                partialCount.FinishBatch(emptyDictionary);
                partialCountCtx.WriteMsgQueueToFile("partial-count.txt");
            }

            {
                LocalContext countSumCtx = LocalContext.Get();
                CountSum countSum = CountSum.Get(countSumCtx, boltParms);

                countSumCtx.ReadFromFileToMsgQueue("partial-count.txt");
                List<SCPTuple> batch = countSumCtx.RecvFromMsgQueue();
                foreach (SCPTuple tuple in batch)
                {
                    countSum.Execute(tuple);
                }
                countSum.FinishBatch(emptyDictionary);
                countSumCtx.WriteMsgQueueToFile("count-sum.txt");
            }
        }
    }

}
