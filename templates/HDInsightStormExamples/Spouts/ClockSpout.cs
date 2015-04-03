using Microsoft.SCP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HDInsightStormExamples.Spouts
{
    /// <summary>
    /// This class is an example for generating TickTuples by yourself in your topology
    /// You can use any stream id of your choice
    /// Make sure you create only one task of this spout and use allGrouping for the bolts that need TickTuples
    /// OR
    /// You can also configure your bolt to send TickTuples by using this property:
    /// "topology.tick.tuple.freq.secs" = "1" or any other number
    /// For SCP.Net to receive TickTuples you need to add SYSTEM_TICK_STREAM_ID into the input Streams
    /// inputSchema.Add(Constants.SYSTEM_TICK_STREAM_ID, new List<Type>() { typeof(object) });
    /// </summary>
    class ClockSpout : ISCPSpout
    {
        Context context;
        long seqId = 0;
        Stopwatch stopwatch;

        public ClockSpout(Context context, Dictionary<string, object> parms = null)
        {
            this.context = context;

            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.SYSTEM_TICK_STREAM_ID, new List<Type>() { 
                typeof(int) });

            this.context.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));

            if(Context.TopologyContext.GetComponentTasks(Context.TopologyContext.GetThisComponentId()).Count != 1)
            {
                throw new Exception("ClockSpout should have only 1 task and you should use allGrouping for it.");
            }

            stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// The NextTuple method of a spout
        /// </summary>
        /// <param name="parms"></param>
        public void NextTuple(Dictionary<string, object> parms)
        {
            if (stopwatch.ElapsedMilliseconds >= 1000)
            {
                this.context.Emit(Constants.SYSTEM_TICK_STREAM_ID, new Values(1), seqId++);
            }
            else
            {
                //Sleep a little
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// The ack method of a spout
        /// </summary>
        /// <param name="seqId">The sequence id of the tuple</param>
        /// <param name="parms"></param>
        public void Ack(long seqId, Dictionary<string, object> parms)
        {
            //do nothing
        }

        /// <summary>
        /// The fail method of a spout
        /// </summary>
        /// <param name="seqId">The sequence id of the tuple</param>
        /// <param name="parms"></param>
        public void Fail(long seqId, Dictionary<string, object> parms)
        {
            //do nothing
        }
    }
}