using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.SCP;
using Microsoft.SCP.Rpc.Generated;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace EventHubReader
{
    /// <summary>
    /// A bolt that reads JSON string values
    /// and writes data to Azure Table Storage
    /// </summary>
    public class Bolt : ISCPBolt
    {
        //per-instance context
        private Context ctx;
        //For accessing Table storage
        private CloudTable table;

        /// <summary>
        /// Bolt constructor
        /// </summary>
        /// <param name="ctx">Topology context</param>
        public Bolt(Context ctx)
        {
            //Topology context > instance context
            this.ctx = ctx;

            //Define the input stream
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            //Stream contains string values
            inputSchema.Add("default", new List<Type>() { typeof(string) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));
            //Use a custom deserializer
            this.ctx.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());
            //Connect to storage
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Properties.Settings.Default.StorageConnection);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            //Create a table named 'events' if it doesn't already exist
            table = tableClient.GetTableReference(Properties.Settings.Default.TableName);
            table.CreateIfNotExists();
        }
        /// <summary>
        /// Returns a new instance Bolt instance
        /// </summary>
        /// <param name="ctx">Topology context</param>
        /// <param name="parms">Parameters</param>
        /// <returns></returns>
        public static Bolt Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new Bolt(ctx);
        }
        /// <summary>
        /// Process inbound data
        /// </summary>
        /// <param name="tuple">A tuple from the data stream</param>
        public void Execute(SCPTuple tuple)
        {
            Context.Logger.Info("Processing events");
            //Get the string tuple value
            string eventValue = (string)tuple.GetValue(0);
            if (eventValue != null)
            {
                //Use Json.NET to parse the JSON string
                JObject eventData = JObject.Parse(eventValue);
                //Create a new Device entity with deviceId
                //from the inboudn JSON as the Partition Key
                Device device = new Device((int)eventData["deviceId"]);
                //Set the value to deviceValue from the inbound JSON
                device.value = (int)eventData["deviceValue"];
                //Insert into the table
                //NOTE: In production this could be improved by using batch inserts
                TableOperation insertOperation = TableOperation.Insert(device);
                table.Execute(insertOperation);
            }
        }
    }
}