using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;

using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;

namespace Scp.App.TxKafkaPro
{
    public class KafkaSpout : ISCPTxSpout
    {
        private string zkAddr;
        private string statPath = "TxKafkaPro";
        private StateStore stateStore;
        private Context ctx;
        private string regName = "KafkaMeta";

        private Random rand = new Random();

        public KafkaSpout(Context ctx)
        {
            Context.Logger.Info("KafkaSpout constructor called");
            this.ctx = ctx;

            if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
            {
                zkAddr = GetZkAddr();
                Console.Write("zookeeper address: " + zkAddr);
                stateStore = StateStore.Get(statPath, zkAddr);
            }
            
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add("mydefault", new List<Type>() { typeof(KafkaMeta) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));
        }

        public void NextTx(out long seqId, Dictionary<string, Object> parms)
        {
            Context.Logger.Info("NextTx enter");

            KafkaMeta meta = (KafkaMeta)parms[Constants.KAFKA_META];

            // the previous kafka meta which get from zookeeper
            KafkaMeta preMeta = null;
            string regKey = "LastSend-" + meta.Topic;
            var reg = stateStore.Get<Registry>(regName);
            if (reg.ExistsKey(regKey))
            {
                preMeta = reg.GetKeyValue<KafkaMeta>(regKey);
            }
            else
            {
                reg.CreateKey(regKey);
            }

            bool isReady = false;
            if (preMeta != null)
            {
                foreach (int i in preMeta.PartitionOffsets.Keys)
                {
                    long preEndOffset = preMeta.PartitionOffsets[i].EndOffset;
                    // set current begin offset by previous endoffset from stored kafka meta
                    meta.PartitionOffsets[i].BeginOffset = preEndOffset;
                    long endOffset = meta.PartitionOffsets[i].EndOffset;
                    Context.Logger.Info(String.Format("For partition {0}, the begin offset {1}, the end offset {2}", i, preEndOffset, endOffset));
                    if (endOffset > preEndOffset)
                    {
                        isReady = true;
                    }
                }
            }
            else
            {
                isReady = true;
                Context.Logger.Info("preMeta is null");
            }

            if (isReady)
            {
                State state = stateStore.Create();
                Context.Logger.Info("stateid in spout {0}", state.ID);
                meta.StateId = BitConverter.GetBytes(state.ID);
                // emit kafka meta
                this.ctx.Emit("mydefault", new Values(meta));
                // save kafka meta to zookeeper
                reg.SetKeyValue(regKey, meta);

                seqId = state.ID;
                Context.Logger.Info("NextTx exit, seqId: {0}", seqId);
            } 
            else
            {
                seqId = -1L;
            }

            System.Threading.Thread.Sleep(1000);
        }

        public void Ack(long seqId, Dictionary<string, Object> parms)
        {
            Context.Logger.Info("Ack, seqId: {0}", seqId);

            if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
            {
                State state = stateStore.GetState(seqId);
                state.Commit(true);
            }
        }

        public void Fail(long seqId, Dictionary<string, Object> parms)
        {
            Context.Logger.Info("Fail, seqId: {0}", seqId);
        }

        public static KafkaSpout Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new KafkaSpout(ctx);
        }

        private string GetZkAddr()
        {
            StringBuilder zkAddr = new StringBuilder();

            int zkPort;
            if (Context.Config.stormConf.ContainsKey(Constants.STORM_ZOOKEEPER_PORT))
            {
                zkPort = (int)(Context.Config.stormConf[Constants.STORM_ZOOKEEPER_PORT]);
                Context.Logger.Info("zkPort: {0}", zkPort);
            }
            else
            {
                throw new Exception("Can't find storm.zookeeper.port");
            }

            if (Context.Config.stormConf.ContainsKey(Constants.STORM_ZOOKEEPER_SERVERS))
            {
                ArrayList zkServers = (ArrayList)(Context.Config.stormConf[Constants.STORM_ZOOKEEPER_SERVERS]);
                Context.Logger.Info("zkServers: {0}", zkServers);

                bool first = true;
                foreach (string host in zkServers)
                {
                    Context.Logger.Info("host: {0}", host);
                    if (!first)
                    {
                        zkAddr.Append(",");
                    }
                    zkAddr.Append(host);
                    zkAddr.Append(":");
                    zkAddr.Append(zkPort);
                    first = false;
                }
            }
            else
            {
                throw new Exception("Can't find storm.zookeeper.servers");
            }

            Context.Logger.Info("zkAddr: {0}", zkAddr);
            return zkAddr.ToString();
        }
    }
}
