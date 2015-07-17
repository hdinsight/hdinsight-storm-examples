using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;

namespace Scp.App.TxKafkaPro
{
    public class CountSum : ISCPBatchBolt
    {
        public static int totalCount = 0;
        public static long lastCommittedTxId = -1;

        private Context ctx;
        private StormTxAttempt txAttempt;
        private int count = 0;

        public CountSum(Context ctx, StormTxAttempt txAttempt)
        {
            Context.Logger.Info("CountSum constructor called, TxId: {0}, AttemptId: {1}",
                txAttempt.TxId, txAttempt.AttemptId);

            this.ctx = ctx;
            this.txAttempt = txAttempt;

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add("mydefault", new List<Type>() { typeof(int) });
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add("mydefault", new List<Type>() { typeof(int) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));
        }

        public void Execute(SCPTuple tuple)
        {
            int cnt = tuple.GetInteger(0);
            this.count += cnt;
            Context.Logger.Info("CountSum, Execute(), cnt: {0}, this.count: {1}", cnt, this.count);
        }

        public void FinishBatch(Dictionary<string, Object> parms)
        {
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
            this.ctx.Emit("mydefault", new Values(totalCount));
        }

        public static CountSum Get(Context ctx, Dictionary<string, Object> parms)
        {
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