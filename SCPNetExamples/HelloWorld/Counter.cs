using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;
using System.Diagnostics;

namespace Scp.App.HelloWorld
{
    /// <summary>
    /// The bolt "counter" uses a dictionary to record the occurrence number of each word.
    /// </summary>
    public class Counter : ISCPBolt
    {
        private Context ctx;
        private bool enableAck = false;
        private int taskIndex = -1;

        private Dictionary<string, int> counts = new Dictionary<string, int>();

        public Counter(Context ctx)
        {
            Context.Logger.Info("Counter constructor called");

            this.ctx = ctx;

            // Declare Input and Output schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add("default", new List<Type>() { typeof(string), typeof(char) });

            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add("default", new List<Type>() { typeof(string), typeof(int) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));

            // Demo how to get pluginConf info and enable ACK in Non-Tx topology
            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);

            //Demo how to get TopologyContext info
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
            Context.Logger.Info("Execute enter");

            string word = tuple.GetString(0);
            int count = counts.ContainsKey(word) ? counts[word] : 0;
            count++;
            counts[word] = count;

            Context.Logger.Info("Emit: {0}, count: {1}", word, count);
            this.ctx.Emit(Constants.DEFAULT_STREAM_ID, new List<SCPTuple> { tuple }, new Values(word, count));

            if (enableAck)
            {
                Context.Logger.Info("Ack tuple: tupleId: {0}", tuple.GetTupleId());
                this.ctx.Ack(tuple);
            }

            // log some info to out file for bvt test validataion
            if (taskIndex == 0) // For component with multiple parallism, only one of them need to log info 
            {
                string fileName = @"..\..\..\..\..\HelloWorldOutput" + Process.GetCurrentProcess().Id  + ".txt";
                FileStream fs = new FileStream(fileName, FileMode.Append);
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine("word: {0}, count: {1}", word, count);
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
        public static Counter Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new Counter(ctx);
        }
    }
}