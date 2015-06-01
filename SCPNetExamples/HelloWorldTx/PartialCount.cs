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
    /// <summary>
    /// The bolt "partial-count" will first get the file name from the received tuple, 
    /// then open the file and count the number of words in this file, 
    /// and finally emit the word number to the "count-sum" bolt. 
    /// </summary>
    public class PartialCount : ISCPBatchBolt
    {
        private Context ctx;

        public PartialCount(Context ctx, StormTxAttempt txAttempt)
        {
            Context.Logger.Info("PartialCount constructor called, TxId: {0}, AttemptId: {1}",
                txAttempt.TxId, txAttempt.AttemptId);

            this.ctx = ctx;

            // Declare Input and Output schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add("default", new List<Type>() { typeof(string) });

            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add("default", new List<Type>() { typeof(int) });
            ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));
        }

        /// <summary>
        /// The Execute() function will be called, when a new tuple is available.
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            int wordCnt = 0;
            string fileName = tuple.GetString(0);
            Context.Logger.Info("PartialCount, Execute(), tuple content: {0}", fileName);

            using (StreamReader reader = new StreamReader(fileName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    Context.Logger.Info("read line: {0}", line);
                    foreach (string word in line.Split(' '))
                    {
                        wordCnt++;
                    }
                }
            }

            Context.Logger.Info("Execute(), wordCnt: {0}", wordCnt);
            this.ctx.Emit(new Values(wordCnt));
        }

        /// <summary>
        /// FinishBatch() is called when this transaction is ended.
        /// </summary>
        /// <param name="parms"></param>
        public void FinishBatch(Dictionary<string, Object> parms)
        {
            Context.Logger.Info("PartialCount, FinishBatch()");
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt, and a Storm_Tx_Attempt object is required for ISCPBatchBolt</param>
        /// <returns></returns>
        public static PartialCount Get(Context ctx, Dictionary<string, Object> parms)
        {
            // for transactional topology, we can get txAttempt from the input parms
            if (parms.ContainsKey(Constants.STORM_TX_ATTEMPT))
            {
                StormTxAttempt txAttempt = (StormTxAttempt)parms[Constants.STORM_TX_ATTEMPT];
                return new PartialCount(ctx, txAttempt);
            }
            else
            {
                throw new Exception("null txAttempt");
            }
        }

    }
}