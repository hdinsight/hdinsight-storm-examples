using System;
using System.Collections.Generic;
using Microsoft.SCP;
using System.Diagnostics;

namespace EventHubsReaderTopology
{
    /// <summary>
    /// Partially count number of messages from EventHubs
    /// </summary>
    public class PartialCountBolt : ISCPBolt
    {
        Context ctx;

        long partialCount = 0L;
        long totalCount = 0L;

        //Maintain a queue of tuples in the current batch
        //We need to ack these tuples when the batch is finished i.e. when the TICK tuple arrives
        //Why queue? - So that we can ack in order the tuples were received
        Queue<SCPTuple> tuplesToAck = new Queue<SCPTuple>();

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
                    //emit with anchors set the tuples in this batch
                    this.ctx.Emit(Constants.DEFAULT_STREAM_ID, tuplesToAck, new Values(partialCount));
                    //ideally in the logs partialCount and the batch count will match
                    Context.Logger.Info("acking the batch: " + tuplesToAck.Count);
                    foreach (var t in tuplesToAck)
                    {
                        this.ctx.Ack(t);
                    }
                    //once all the tuples are acked, clear the batch
                    tuplesToAck.Clear();
                    partialCount = 0L;
                }
            }
            else
            {
                partialCount++;
                totalCount++;
                //Do no ack here but add to the acking queue
                tuplesToAck.Enqueue(tuple);
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