using EventHubAggregatorToHBaseTopology.Common;
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

namespace EventHubAggregatorToHBaseTopology
{
    /// <summary>
    /// This class in an example of ack based non transactional Spout
    /// You can club this spout with EventHubs Java bolt
    /// </summary>
    class EventGenerator : ISCPSpout
    {
        Context context;
        AppConfig appConfig;

        long lastseqid = 0;

        long global_emit_count = 0;

        Stopwatch globalstopwatch;

        JsonSerializer jsonserializer;

        Dictionary<long, string> spoutCache = new Dictionary<long, string>();
        bool ackEnabled = false;

        public EventGenerator(Context context, Dictionary<string, object> parms = null)
        {
            this.context = context;

            this.appConfig = new AppConfig();

            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });

            this.context.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));

            //This statement is used for Hybrid scenarios where you will add a customized serializer in C# spout 
            //and a customized deserializer in your java bolt
            this.context.DeclareCustomizedSerializer(new CustomizedInteropJSONSerializer());

            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                ackEnabled = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }

            globalstopwatch = new Stopwatch();
            globalstopwatch.Start();

            //To serialize WebRequestLog into a JSON to send into EventHub
            jsonserializer = JsonSerializer.Create();
        }

        /// <summary>
        /// EventGenerator contructor delegate that SCP.Net uses to invoke the instance of this class
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parms"></param>
        /// <returns>Instance of EventGenerator</returns>
        public static EventGenerator Get(Context context, Dictionary<string, Object> parms)
        {
            return new EventGenerator(context, parms);
        }

        /// <summary>
        /// The ack method of a spout
        /// </summary>
        /// <param name="seqId"></param>
        /// <param name="parms"></param>
        public void Ack(long seqId, Dictionary<string, object> parms)
        {
            if (ackEnabled)
            {
                //Remove the successfully acked tuple from the cache.
                spoutCache.Remove(seqId);
            }
        }

        /// <summary>
        /// The fail method of a spout
        /// </summary>
        /// <param name="seqId"></param>
        /// <param name="parms"></param>
        public void Fail(long seqId, Dictionary<string, object> parms)
        {
            if (ackEnabled)
            {
                //Re-emit the failed tuple again - only if it exists
                if(spoutCache.ContainsKey(seqId))
                {
                    this.context.Emit(Constants.DEFAULT_STREAM_ID, new Values(spoutCache[seqId]), seqId);
                }
            }
        }

        public void NextTuple(Dictionary<string, object> parms)
        {
            lastseqid++;

            //Get a random WebRequestLog
            var json = WebRequestLog.GetRandomWebRequestLogAsJson();

            if (ackEnabled)
            {
                //Add to the spout cache so that the tuple can be re-emitted on fail
                spoutCache.Add(lastseqid, json);
            }

            this.context.Emit(Constants.DEFAULT_STREAM_ID, new Values(json), lastseqid);

            global_emit_count++;

            if (global_emit_count % 5000 == 0)
            {
                Context.Logger.Info("Last emitted tuple: SeqId = {0} Value = {1}", lastseqid, json);
                Context.Logger.Info("Total tuples emitted: {0} ({1} tuples/sec)", global_emit_count, global_emit_count / globalstopwatch.Elapsed.TotalSeconds);
            }
        }
    }
}