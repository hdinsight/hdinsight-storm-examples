using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections;

using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;

namespace Scp.App.HelloWorldTx
{
    /// <summary>
    /// The spout "generator" will randomly choose two files from the pre-created three files, and emit the two file names to the "partial-count" bolt. 
    /// There are also three pre-created txt files: "DataSource0.txt", "DataSource1.txt" and "DataSource2.txt". 
    /// </summary>
    public class Generator : ISCPTxSpout
    {
        private string zkAddr;
        private string zkRoot;
        private string statPath = "HelloWorldTx";
        private StateStore stateStore;
        private Context ctx;

        private Random rand = new Random();
        private long lastSeqId = 0;
        private string[] dataSourceFiles = { "DataSource0.txt", "DataSource1.txt", "DataSource2.txt" };

        public Generator(Context ctx)
        {
            Context.Logger.Info("Generator constructor called");
            this.ctx = ctx;

            // Declare Output schema
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add("default", new List<Type>() { typeof(string) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));

            // Demo how to use StateStore in Tx topology to persistence some states
            if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
            {
                // Get zookeeper address from storm conf, which is passed from java side
                zkAddr = GetZkAddr();
                zkRoot = GetZkRoot();
                stateStore = StateStore.Get(zkRoot + "/" + statPath, zkAddr);
            }
        }

        /// <summary>
        /// NextTx() is called to start a new transaction, the out parameter “seqId” is used to identify the transaction, 
        ///     which is also used in Ack() and Fail(). In NextTx(), user can emit data to Java side. 
        /// The data will be stored in ZooKeeper to support replay. Because the capacity of ZooKeeper is very limited, 
        ///     user should only emit metadata, not bulk data in transactional spout.
        /// 
        /// Just like their non-transactional counter-part, NextTx(), Ack(), and Fail() are all called in a tight loop in a single thread in C# process. 
        /// When there are no data to emit, it is courteous to have NextTx sleep for a short amount of time (10 milliseconds) so as not to waste too much CPU.
        /// </summary>
        /// <param name="seqId">Sequence Id of the tuple</param>
        /// <param name="parms"></param>
        public void NextTx(out long seqId, Dictionary<string, Object> parms)
        {
            Context.Logger.Info("NextTx enter");

            for (int i = 0; i < 2; i++)
            {
                string filename = dataSourceFiles[rand.Next(0, 3)];
                this.ctx.Emit(new Values(filename));
                Context.Logger.Info("Emit: {0}", filename);
            }

            if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
            {
                State state = stateStore.Create();
                seqId = state.ID;
            }
            else
            {
                lastSeqId++;
                seqId = lastSeqId;
            }
            
            Context.Logger.Info("NextTx exit, seqId: {0}", seqId);
        }

        /// <summary>
        /// Ack() will be called when the transaction is committed.
        /// </summary>
        /// <param name="seqId">Sequence Id of the tuple which is acked.</param>
        /// <param name="parms"></param>
        public void Ack(long seqId, Dictionary<string, Object> parms)
        {
            Context.Logger.Info("Ack, seqId: {0}", seqId);

            if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
            {
                State state = stateStore.GetState(seqId);
                state.Commit(true);
            }
        }

        /// <summary>
        /// Reserved method, not been implemented.
        /// Storm will replay a transaction automatically if it fails, so Fail() should not be called in normal case. 
        /// This method is reserved for add new features, such as metadata validataion or something else.
        /// </summary>
        /// <param name="seqId">Sequence Id of the tuple which is failed.</param>
        /// <param name="parms"></param>
        public void Fail(long seqId, Dictionary<string, Object> parms)
        {
            Context.Logger.Info("Fail, seqId: {0}", seqId);
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt</param>
        /// <returns></returns>
        public static Generator Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new Generator(ctx);
        }

        /// <summary>
        /// Examples to get Zookeeper address from storm.yaml config.
        /// Users also can just use their own Zookeepers here.
        /// </summary>
        /// <returns>Zookeeper connect string</returns>
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

        /// <summary>
        /// Examples to get Zookeeper root path from storm.yaml config.
        /// Users also can just use their own Zookeepers and set to any zk path here.
        /// </summary>
        /// <returns>Zookeeper connect string</returns>
        private string GetZkRoot()
        {
            string zkRoot = "/TEMPEST";

            if (Context.Config.stormConf.ContainsKey(Constants.STORM_ZOOKEEPER_ROOT))
            {
                string stormRoot = (string)Context.Config.stormConf[Constants.STORM_ZOOKEEPER_ROOT];
                int rootPos = stormRoot.LastIndexOf('/');
                if (rootPos > 0)
                {
                    zkRoot = stormRoot.Substring(0, rootPos) + zkRoot;
                }
            }

            Context.Logger.Info("zkRoot: {0}", zkRoot);
            return zkRoot;
        }
    }
}