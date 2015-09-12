using Microsoft.SCP;
using System;
using System.Collections.Generic;

namespace SqlAzureWriterStormApplication
{
    /// <summary>
    /// IISLogGeneratorSpout - A SCP.Net C# Bolt that emits random IIS Logs for upstream tasks.
    /// This is a non-transactional spout that can operate with or without acks
    /// In enableAck = true mode, it caches the tuples and re-emits them on Fail
    /// </summary>
    public class IISLogGeneratorSpout : ISCPSpout
    {
        Context context;
        long seqId = 0;

        Dictionary<long, List<object>> cachedTuples = new Dictionary<long, List<object>>();
        bool enableAck = false;

        public static Random random = new Random();

        public IISLogGeneratorSpout(Context context, Dictionary<string, object> parms = null)
        {
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the outgoing tuples for the upstream bolt tasks
            //As this is a spout, you can set inputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>();
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, OutputFieldTypes);

            this.context.DeclareComponentSchema(new ComponentStreamSchema(null, outputSchema));

            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);
        }

        /// <summary>
        /// IISLogGeneratorSpout contructor delegate that SCP.Net uses to invoke the instance of this class
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parms"></param>
        /// <returns>Instance of IISLogGeneratorSpout</returns>
        public static IISLogGeneratorSpout Get(Context context, Dictionary<string, Object> parms)
        {
            return new IISLogGeneratorSpout(context, parms);
        }

        public static List<string> RandomClientIps = new List<string>() { 
            "127.0.0.1", "10.9.8.7", "192.168.1.1", "255.255.255.255", "123.123.123.123" };
        public static List<string> RandomUris = new List<string>() { 
            "/foo", "/bar", "/foo/bar", "/spam", "/eggs", "/spam/eggs" };
        public static List<string> RandomMethods = new List<string>() { 
            "GET", "POST", "PUT", "DELETE" };
        public static List<int> RandomResponses = new List<int>() { 
            200, 400, 404, 500};

        //Handy list to provide names to fields when building the topology in TopologyBuilder
        public static List<string> OutputFields = new List<string>() { 
            "Timestamp", "ClientIP", "UriStem", "Method", "Response" };
        //Handy list to provide types of fields for other tasks that will consume
        public static List<Type> OutputFieldTypes = new List<Type>() { 
            typeof(DateTime), typeof(string), typeof(string), typeof(string), typeof(int) };

        /// <summary>
        /// Generate a random IISLog
        /// </summary>
        /// <returns></returns>
        public static Values GetRandomIISLog()
        {
            var values = new Values(
                DateTime.UtcNow,
                RandomClientIps[random.Next(RandomClientIps.Count)],
                RandomUris[random.Next(RandomUris.Count)],
                RandomMethods[random.Next(RandomMethods.Count)],
                RandomResponses[random.Next(RandomResponses.Count)]);
            return values;
        }

        /// <summary>
        /// The NextTuple method of a spout
        /// </summary>
        /// <param name="parms"></param>
        public void NextTuple(Dictionary<string, object> parms)
        {
            var iisLog = GetRandomIISLog();
            if (enableAck)
            {
                //Add to the spout cache so that the tuple can be re-emitted on fail
                cachedTuples.Add(seqId, iisLog);
                this.context.Emit(Constants.DEFAULT_STREAM_ID, iisLog, seqId);
                seqId++;
            }
            else
            {
                this.context.Emit(Constants.DEFAULT_STREAM_ID, iisLog);
            }
        }

        /// <summary>
        /// The ack method of a spout
        /// </summary>
        /// <param name="seqId">The sequence id of the tuple</param>
        /// <param name="parms"></param>
        public void Ack(long seqId, Dictionary<string, object> parms)
        {
            if (enableAck)
            {
                //Remove the successfully acked tuple from the cache.
                cachedTuples.Remove(seqId);
            }
        }

        /// <summary>
        /// The fail method of a spout
        /// </summary>
        /// <param name="seqId">The sequence id of the tuple</param>
        /// <param name="parms"></param>
        public void Fail(long seqId, Dictionary<string, object> parms)
        {
            if (enableAck)
            {
                //Re-emit the failed tuple again - only if it exists
                if (cachedTuples.ContainsKey(seqId))
                {
                    this.context.Emit(Constants.DEFAULT_STREAM_ID, cachedTuples[seqId], seqId);
                }
            }
        }
    }
}