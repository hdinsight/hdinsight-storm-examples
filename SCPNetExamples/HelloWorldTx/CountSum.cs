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
    /// The "count-sum" bolt will summarize the total count.
    /// </summary>
    public class CountSum : ISCPBatchBolt
    {
        public static int totalCount = 0;
        public static long lastCommittedTxId = -1;

        private Context ctx;
        private StormTxAttempt txAttempt;
        private int count = 0;

        private int taskIndex = -1;

        public CountSum(Context ctx, StormTxAttempt txAttempt)
        {
            Context.Logger.Info("CountSum constructor called, TxId: {0}, AttemptId: {1}",
                txAttempt.TxId, txAttempt.AttemptId);

            this.ctx = ctx;
            this.txAttempt = txAttempt;

            // Declare Input and Output schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add("default", new List<Type>() { typeof(int) });

            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add("default", new List<Type>() { typeof(int) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));

            //demo how to get TopologyContext info
            if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
            {
                Context.Logger.Info("TopologyContext info:");
                TopologyContext topologyContext = Context.TopologyContext;
                Context.Logger.Info("taskId: {0}", topologyContext.GetThisTaskId());
                taskIndex = topologyContext.GetThisTaskIndex();
                Context.Logger.Info("taskIndex: {0}", taskIndex);
                string componentId = topologyContext.GetThisComponentId();
                Context.Logger.Info("componentId: {0}", componentId);
                List<int> componentTasks = topologyContext.GetComponentTasks(componentId);
                Context.Logger.Info("taskNum: {0}", componentTasks.Count);
            }
        }

        /// <summary>
        /// The Execute() function will be called, when a new tuple is available.
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            int cnt = tuple.GetInteger(0);
            this.count += cnt;
            Context.Logger.Info("CountSum, Execute(), cnt: {0}, this.count: {1}", cnt, this.count);
        }

        /// <summary>
        /// FinishBatch() is called when this transaction is ended.
        /// </summary>
        /// <param name="parms"></param>
        public void FinishBatch(Dictionary<string, Object> parms)
        {
            // Check if this TxId has been commited, because this transaction may be replayed in multiple times
            bool replay = (this.txAttempt.TxId <= lastCommittedTxId);
            Context.Logger.Info("FinishBatch(), lastCommittedTxId: {0}, TxId: {1}, replay: {2}",
                lastCommittedTxId, txAttempt.TxId, replay);

            if (!replay)
            {
                totalCount = totalCount + this.count;
                lastCommittedTxId = this.txAttempt.TxId;
            }

            Context.Logger.Info("CountSum, FinishBatch(), count: {0}, totalCount: {1}",
                this.count, totalCount);
            this.ctx.Emit(new Values(totalCount));

            // Log some info to out file for bvt test validataion
            if (taskIndex == 0) // For component with multiple parallism, only one of them need to log info 
            {
                string fileName = @"..\..\..\..\..\HelloWorldTxOutput.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append);
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine("count: {0}, totalCount: {1}", count, totalCount);
                }
            }
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt, and a Storm_Tx_Attempt object is required for ISCPBatchBolt</param>
        /// <returns></returns>
        public static CountSum Get(Context ctx, Dictionary<string, Object> parms)
        {
            // for transactional topology, we can get txAttempt from the input parms
            if (parms.ContainsKey(Constants.STORM_TX_ATTEMPT))
            {
                StormTxAttempt txAttempt = (StormTxAttempt)parms[Constants.STORM_TX_ATTEMPT];
                return new CountSum(ctx, txAttempt);
            }
            else
            {
                throw new Exception("null txAttempt");
            }
        }
    }
}