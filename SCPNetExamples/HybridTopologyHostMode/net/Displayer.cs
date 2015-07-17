using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;
using System.Diagnostics;

namespace Scp.App.HybridTopologyHostMode
{
    /// <summary>
    /// The Non-Tx bolt "displayer" print the Person info to logs.
    /// </summary>
    public class Displayer : ISCPBolt
    {
        private Context ctx;
        private int taskIndex = -1;

        public Displayer(Context ctx)
        {
            Context.Logger.Info("Counter constructor called");

            this.ctx = ctx;

            // Declare Input schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add("default", new List<Type>() { typeof(Person) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));
            this.ctx.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            //Demo how to get TopologyContext info
            if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
            {
                taskIndex = Context.TopologyContext.GetThisTaskIndex();
                Context.Logger.Info("taskIndex: {0}", taskIndex);
            }
        }

        /// <summary>
        /// The Execute() function will be called, when a new tuple is available.
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            Context.Logger.Info("Execute enter");

            Person person = (Person)tuple.GetValue(0);
            Context.Logger.Info("person: {0}", person.ToString());

            // log some info to out file for bvt test validataion
            if (taskIndex == 0) // For component with multiple parallism, only one of them need to log info 
            {
                string fileName = @"..\..\..\..\..\HybridTopologyOutput" + Process.GetCurrentProcess().Id + ".txt";
                FileStream fs = new FileStream(fileName, FileMode.Append);
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine("person: {0}", person.ToString());
                }
            }

            Context.Logger.Info("Execute exit");

        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt</param>
        /// <returns></returns>
        public static Displayer Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new Displayer(ctx);
        }
    }

    /// <summary>
    /// The Tx bolt "displayer" print the Person info to logs.
    /// </summary>
    public class TxDisplayer : ISCPBatchBolt
    {
        public static long lastCommittedTxId = -1;

        private Context ctx;
        private StormTxAttempt txAttempt;
        private int taskIndex = -1;

        public TxDisplayer(Context ctx, StormTxAttempt txAttempt)
        {
            Context.Logger.Info("TxDisplayer constructor called, TxId: {0}, AttemptId: {1}",
                txAttempt.TxId, txAttempt.AttemptId);

            this.ctx = ctx;
            this.txAttempt = txAttempt;

            // Declare Input schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add("default", new List<Type>() { typeof(Person) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));
            this.ctx.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            //Demo how to get TopologyContext info
            if (Context.pluginType != SCPPluginType.SCP_NET_LOCAL)
            {
                taskIndex = Context.TopologyContext.GetThisTaskIndex();
                Context.Logger.Info("taskIndex: {0}", taskIndex);
            }
        }

        /// <summary>
        /// The Execute() function will be called, when a new tuple is available.
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            Context.Logger.Info("Execute enter");
            Person person = (Person)tuple.GetValue(0);
            Context.Logger.Info("person: {0}", person.ToString());

            // log some info to out file for bvt test validataion
            if (taskIndex == 0) // For component with multiple parallism, only one of them need to log info 
            {
                string fileName = @"..\..\..\..\..\HybridTopologyOutput" + Process.GetCurrentProcess().Id + ".txt";
                FileStream fs = new FileStream(fileName, FileMode.Append);
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine("person: {0}", person.ToString());
                }
            }

            Context.Logger.Info("Execute exit");
        }

        /// <summary>
        /// FinishBatch() is called when this transaction is ended.
        /// </summary>
        /// <param name="parms"></param>
        public void FinishBatch(Dictionary<string, Object> parms)
        {
            bool replay = (this.txAttempt.TxId <= lastCommittedTxId);
            Context.Logger.Info("FinishBatch(), lastCommittedTxId: {0}, TxId: {1}, replay: {2}",
                lastCommittedTxId, txAttempt.TxId, replay);
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt, and a Storm_Tx_Attempt object is required for ISCPBatchBolt</param>
        /// <returns></returns>
        public static TxDisplayer Get(Context ctx, Dictionary<string, Object> parms)
        {
            // for transactional topology, we can get txAttempt from the input parms
            if (parms.ContainsKey(Constants.STORM_TX_ATTEMPT))
            {
                StormTxAttempt txAttempt = (StormTxAttempt)parms[Constants.STORM_TX_ATTEMPT];
                return new TxDisplayer(ctx, txAttempt);
            }
            else
            {
                throw new Exception("null txAttempt");
            }
        }
    }

}