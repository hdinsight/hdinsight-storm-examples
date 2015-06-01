using Microsoft.SCP;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace TestStormApplicationTemplates
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
        public LoggerBolt(Context context, Dictionary<string, Object> parms)
        {
            //Set the context
            this.context = context;

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            inputSchema.Add(Constants.DEFAULT_STREAM_ID, (List<Type>)parms["inputSchema"]);

            //Declare both input and output schemas
            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));
        }

        public static LoggerBolt Get(Context context, Dictionary<string, Object> parms)
        {
            return new LoggerBolt(context, parms);
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