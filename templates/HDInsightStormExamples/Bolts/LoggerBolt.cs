using HDInsightStormExamples.Spouts;
using Microsoft.SCP;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace HDInsightStormExamples.Bolts
{
    /// <summary>
    /// A simple bolt that logs the values of incoming tuples (and also as JSON).
    /// </summary>
    public class LoggerBolt : ISCPBolt
    {
        Context context;
        //A local flag to indicate if the bolt needs to ack on tuples
        bool enableAck = false;

        long count = 0;

        //Constructor
        public LoggerBolt(Context context)
        {
            Context.Logger.Info(this.GetType().Name + " constructor called");
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the incoming tuples from the downstream spout or bolt tasks
            //You will also need to declare the schema for the any outgoing tuples to the upstream spout or bolt tasks
            //If there are no outgoing tuples, you can set outputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });
            //Or, something like this if you have multiple fields
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(int), typeof(DateTime), typeof(string) });
            //Another way is to have the OutputFieldTypes list exposed from Spout or Bolt to make it easy to tie with bolts that will consume it
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, IISLogGeneratorSpout.OutputFieldTypes);
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpout.OutputFieldTypes);

            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { 
                typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) });

            //Declare both input and output schemas
            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            //If this task excepts acks we need to set enableAck as true in TopologyBuilder for it
            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);

            //TODO: Uncomment if using in hybrid mode (Java -> C#) - We need to Deserialize Java objects into C# objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaSerializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" } )
            //If the incoming tuples have only string fields, this is optional
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());
        }

        public static LoggerBolt Get(Context context, Dictionary<string, Object> parms)
        {
            return new LoggerBolt(context);
        }

        public void Execute(SCPTuple tuple)
        {
            try
            {
                count++;
                var sb = new StringBuilder();
                sb.AppendFormat("Received Tuple {0}: ", count);

                var values = tuple.GetValues();
                for (int i = 0; i < values.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.AppendFormat("{0} = {1}", i, values[i].ToString());
                }
                Context.Logger.Info(sb.ToString());
                Context.Logger.Info("Tuple values as JSON: " +
                    (values.Count == 1 ? JsonConvert.SerializeObject(values[0]) : JsonConvert.SerializeObject(values)));

                //Ack the tuple if enableAck is set to true in TopologyBuilder. This is mandatory if the downstream bolt or spout expects an ack.
                if (enableAck)
                {
                    this.context.Ack(tuple);
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Error("An error occured while executing Tuple Id: {0}. Exception Details:\r\n{1}", 
                    tuple.GetTupleId(), ex.ToString());

                //Fail the tuple if enableAck is set to true in TopologyBuilder so that the tuple is replayed.
                if (enableAck)
                {
                    this.context.Fail(tuple);
                }
            }
        }
    }
}