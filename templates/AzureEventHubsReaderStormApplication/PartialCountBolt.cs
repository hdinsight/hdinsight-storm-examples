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

        public PartialCountBolt(Context ctx)
        {
            this.ctx = ctx;

            // set input schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });

            //Add the Tick tuple Stream in input streams - A tick tuple has only one field of type long
            inputSchema.Add(Constants.SYSTEM_TICK_STREAM_ID, new List<Type>() { typeof(long) });

            // set output schemas
            Dictionary<string, List<Type>> ouputSchema = new Dictionary<string, List<Type>>();
            ouputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(long) });

            // Declare input and output schemas
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, ouputSchema));

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
                    this.ctx.Emit(new Values(partialCount));
                    partialCount = 0L;
                }
            }
            else
            {
                partialCount++;
                totalCount++;
                this.ctx.Ack(tuple);
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