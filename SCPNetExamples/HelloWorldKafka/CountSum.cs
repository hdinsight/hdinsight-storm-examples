using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;

namespace Scp.App.HelloWorld
{
    public class CountSum : ISCPBolt
    {
        private Context ctx;
        private bool enableAck = false;
        private long totalNum = 0;

        public CountSum(Context ctx)
        {
            Context.Logger.Info("Counter constructor called");

            this.ctx = ctx;

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add("default", new List<Type>() { typeof(int) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);
        }

        public void Execute(SCPTuple tuple)
        {
            Context.Logger.Info("Execute enter");

            int bytesNum = tuple.GetInteger(0);
            totalNum += bytesNum;

            Context.Logger.Info("bytesNum: {0}, totalNum: {1}", bytesNum, totalNum);

            if (enableAck)
            {
                Context.Logger.Info("Ack tuple: tupleId: {0}", tuple.GetTupleId());
                this.ctx.Ack(tuple);
            }

            Context.Logger.Info("Execute exit");
        }

        public static CountSum Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new CountSum(ctx);
        }
    }
}