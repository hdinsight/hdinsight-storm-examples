using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;

namespace Scp.App.HelloWorldKafka
{
    public class PartialCount : ISCPBatchBolt
    {
        private Context ctx;
        private int count = 0;

        public PartialCount(Context ctx, StormTxAttempt txAttempt)
        {
            Context.Logger.Info("PartialCount constructor called, TxId: {0}, AttemptId: {1}",
                txAttempt.TxId, txAttempt.AttemptId);
            this.ctx = ctx;

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add("default", new List<Type>() { typeof(byte[]) });
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add("default", new List<Type>() { typeof(int) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));
        }

        public void Execute(SCPTuple tuple)
        {
            count++;
            Context.Logger.Info("Execute(), count: {0}", count);
        }

        public void FinishBatch(Dictionary<string, Object> parms)
        {
            Context.Logger.Info("PartialCount, FinishBatch()");
            Context.Logger.Info("emit partial count: {0}", count);
            this.ctx.Emit(new Values(count));
        }

        public static PartialCount Get(Context ctx, Dictionary<string, Object> parms)
        {
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