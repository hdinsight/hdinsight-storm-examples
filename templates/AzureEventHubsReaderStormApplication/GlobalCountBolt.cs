using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SCP;
using System.Diagnostics;
using System.Data.SqlClient;

namespace EventHubsReaderTopology
{
    /// <summary>
    /// Globally count number of messages
    /// </summary>
    public class GlobalCountBolt : ISCPBolt
    {
        Context ctx;

        long partialCount = 0L;
        long totalCount = 0L;

        public GlobalCountBolt(Context ctx)
        {
            this.ctx = ctx;

            // Declare Input schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(long) });

            //Add the Tick tuple Stream in input streams - A tick tuple has only one field of type long
            inputSchema.Add(Constants.SYSTEM_TICK_STREAM_ID, new List<Type>() { typeof(long) });

            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(long), typeof(long) });

            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));

            partialCount = 0L;
            totalCount = 0L;
        }

        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
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
                    Context.Logger.Info("emitting totalCount" +
                        ", partialCount: " + partialCount +
                        ", totalCount: " + totalCount);
                    this.ctx.Emit(new Values(CurrentTimeMillis(), totalCount));
                    partialCount = 0L;
                }
            }
            else
            {
                //Merge partialCount from all EventCountPartialCountBolt
                var incomingPartialCount = tuple.GetLong(0);
                partialCount += incomingPartialCount;
                totalCount += incomingPartialCount;
            }
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt</param>
        /// <returns></returns>
        public static GlobalCountBolt Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new GlobalCountBolt(ctx);
        }
    }
}