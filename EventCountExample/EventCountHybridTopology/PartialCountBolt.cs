using System;
using System.Collections.Generic;
using Microsoft.SCP;
using System.Diagnostics;

namespace EventCountHybridTopology
{
    /// <summary>
    /// Partially count number of messages from EventHubs
    /// </summary>
    public class PartialCountBolt : ISCPBolt
    {
        Context ctx;

        long partialCount = 0L;
        long totalCount = 0L;

        Queue<SCPTuple> tuplesToAck = new Queue<SCPTuple>();

        bool enableAck = false;

        public PartialCountBolt(Context ctx)
        {
            this.ctx = ctx;

            // set input schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });

            //Add the Tick tuple Stream in input streams - A tick tuple has only one field of type long
            inputSchema.Add(Constants.SYSTEM_TICK_STREAM_ID, new List<Type>() { typeof(long) });

            // set output schemas
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(long) });

            // Declare input and output schemas
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));

            this.ctx.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }

            partialCount = 0L;
            totalCount = 0L;
        }

        /// <summary>
        /// The Execute() function will be called, when a new tuple is available.
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            if (tuple.GetSourceStreamId().Equals(Constants.SYSTEM_TICK_STREAM_ID))
            {
                if (partialCount > 0)
                {
                    Context.Logger.Info("emitting partialCount: " + partialCount +
                        ", totalCount: " + totalCount);
                    if (enableAck)
                    {
                        Context.Logger.Info("tuplesToAck: " + tuplesToAck);
                        this.ctx.Emit(Constants.DEFAULT_STREAM_ID, tuplesToAck, new Values(partialCount));
                        foreach (var tupleToAck in tuplesToAck)
                        {
                            this.ctx.Ack(tupleToAck);
                        }
                        tuplesToAck.Clear();
                    }
                    else
                    {
                        this.ctx.Emit(Constants.DEFAULT_STREAM_ID, new Values(partialCount));
                    }
                    partialCount = 0L;
                }
            }
            else
            {
                partialCount++;
                totalCount++;
                if (enableAck)
                {
                    tuplesToAck.Enqueue(tuple);
                }
            }
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt</param>
        /// <returns></returns>
        public static PartialCountBolt Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new PartialCountBolt(ctx);
        }
    }
}