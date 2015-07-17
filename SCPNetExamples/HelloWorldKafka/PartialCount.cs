using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;

namespace Scp.App.HelloWorld
{
    public class PartialCount : ISCPBolt
    {
        private Context ctx;
        private bool enableAck = false;

        public PartialCount(Context ctx)
        {
            Context.Logger.Info("PartialCount constructor called");
            this.ctx = ctx;

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add("default", new List<Type>() { typeof(byte[]) });
            inputSchema.Add(Constants.SYSTEM_TICK_STREAM_ID, new List<Type>() { typeof(long) });
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add("default", new List<Type>() { typeof(int) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));

            // demo how to get pluginConf info
            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);
        }

        public void Execute(SCPTuple tuple)
        {
            Context.Logger.Info("Execute enter");

            if (Constants.SYSTEM_TICK_STREAM_ID.Equals(tuple.GetSourceStreamId()))
            {
                long data = tuple.GetLong(0);
                Context.Logger.Info("tick tuple, value: {0}", data);
            }
            else
            {
                byte[] data = tuple.GetBinary(0);
                int bytesNum = data.Count();

                if (enableAck)
                {
                    this.ctx.Emit(Constants.DEFAULT_STREAM_ID, new List<SCPTuple> { tuple }, new Values(bytesNum));
                    this.ctx.Ack(tuple);
                    Context.Logger.Info("emit bytesNum: {0}", bytesNum);
                    Context.Logger.Info("Ack tuple: tupleId: {0}", tuple.GetTupleId());
                }
                else
                {
                    this.ctx.Emit(Constants.DEFAULT_STREAM_ID, new Values(bytesNum));
                    Context.Logger.Info("emit bytesNum: {0}", bytesNum);
                }                
            }

            Context.Logger.Info("Execute exit");
        }

        public static PartialCount Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new PartialCount(ctx);
        }
    }
}