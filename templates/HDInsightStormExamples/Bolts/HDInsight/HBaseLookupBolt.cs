using HDInsightStormExamples.Spouts;
using Microsoft.HBase.Client;
using Microsoft.SCP;
using org.apache.hadoop.hbase.rest.protobuf.generated;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace HDInsightStormExamples.Bolts
{
    /// <summary>
    /// OVERVIEW:
    /// This is a HBase bolt that uses the HBase .Net SDK to look up rows from HBase.
    /// The values of the rows are then emitted to upstream bolt tasks. 
    /// A user can choose to emit rowkey by modifying code in execute block.
    /// 
    /// PRE-REQUISITES:
    /// 1. Microsoft HDInsight HBase Cluster and credentials
    /// 2. HBase Table Schema
    /// 
    /// NUGET:
    /// 1. Microsoft.SCP.Net.SDK - http://www.nuget.org/packages/Microsoft.SCP.Net.SDK/
    /// 2. Microsoft.HBase.Client http://www.nuget.org/packages/Microsoft.HBase.Client/
    /// 
    /// ASSUMPTIONS:
    /// 1. The first field of the Tuple is the ROWKEY of HBASE and rest of the fields are equal to number of HBASE table columns defined in AppSettings
    /// 2. Your previous spout or bolt task takes care of the ROWKEY design. You can choose to add that implementation here too.
    /// 3. All the incoming values that are primitive types are automtically converted into byte[]. If you wish to use complex types, you need to modify the code to handle that case.
    ///   a. DateTime is converted to Millis since UNIX epoch
    /// 
    /// REFERENCES:
    /// 1. https://github.com/hdinsight/hbase-sdk-for-net
    /// 2. https://github.com/apache/storm/tree/master/external/storm-hbase
    /// </summary>
    class HBaseLookupBolt : ISCPBolt
    {
        Context context;
        bool enableAck = false;

        string HBaseClusterUrl { get; set; }
        string HBaseClusterUserName { get; set; }
        string HBaseClusterPassword { get; set; }

        ClusterCredentials HBaseClusterCredentials;
        HBaseClient HBaseClusterClient;

        string HBaseTableName { get; set; }

        /// <summary>
        /// HBaseLookupBolt
        /// </summary>
        /// <param name="context"></param>
        public HBaseLookupBolt(Context context)
        {
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the incoming tuples from the downstream spout or bolt tasks
            //You will also need to declare the schema for the any outgoing tuples to the upstream spout or bolt tasks
            //If there are no outgoing tuples, you can set outputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });
            //Another way is to have the OutputFieldTypes list exposed from Spout or Bolt to make it easy to tie with bolts that will consume it
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpout.OutputFieldTypes);

            var outputSchema = new Dictionary<string, List<Type>>();
            //Set as many columns you are expecting to lookup and emit from the HBase table
            outputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) });
            //outputSchema.Add(Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpout.OutputFieldTypes);

            //Declare both input and output schemas
            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));

            //TODO: Uncomment if using in hybrid mode (Java -> C#) - We need to Deserialize Java objects into C# objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaSerializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" } )
            //If the incoming tuples have only string fields, this is optional
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            //TODO: Uncomment if using in reverse hybrid mode (C# -> Java) - We need to Serialize C# objects into Java objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaDeserializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer" } )
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedSerializer(new CustomizedInteropJSONSerializer());

            //If this task excepts acks we need to set enableAck as true in TopologyBuilder for it
            if (Context.Config.pluginConf.ContainsKey(Constants.NONTRANSACTIONAL_ENABLE_ACK))
            {
                enableAck = (bool)(Context.Config.pluginConf[Constants.NONTRANSACTIONAL_ENABLE_ACK]);
            }
            Context.Logger.Info("enableAck: {0}", enableAck);

            InitializeHBase();
        }

        /// <summary>
        /// A delegate method to return the instance of this class
        /// </summary>
        /// <param name="context">SCP Context, automatically passed by SCP.Net</param>
        /// <param name="parms"></param>
        /// <returns>An instance of the current class</returns>
        public static HBaseLookupBolt Get(Context context, Dictionary<string, Object> parms)
        {
            return new HBaseLookupBolt(context);
        }

        /// <summary>
        /// Initialize the HBase settings and connections
        /// </summary>
        public void InitializeHBase()
        {
            this.HBaseClusterUrl = ConfigurationManager.AppSettings["HBaseClusterUrl"];
            if (String.IsNullOrWhiteSpace(this.HBaseClusterUrl))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "HBaseClusterUrl");
            }

            this.HBaseClusterUserName = ConfigurationManager.AppSettings["HBaseClusterUserName"];
            if (String.IsNullOrWhiteSpace(this.HBaseClusterUserName))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "HBaseClusterUserName");
            }

            this.HBaseClusterPassword = ConfigurationManager.AppSettings["HBaseClusterPassword"];
            if (String.IsNullOrWhiteSpace(this.HBaseClusterPassword))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "HBaseClusterPassword");
            }

            this.HBaseTableName = ConfigurationManager.AppSettings["HBaseTableName"];
            if (String.IsNullOrWhiteSpace(this.HBaseTableName))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "HBaseTableName");
            }

            //Setup the credentials for HBase cluster
            this.HBaseClusterCredentials =
                new ClusterCredentials(
                    new Uri(this.HBaseClusterUrl),
                    this.HBaseClusterUserName,
                    this.HBaseClusterPassword);

            this.HBaseClusterClient = new HBaseClient(this.HBaseClusterCredentials);

            //Query HBase for existing tables
            var tables = this.HBaseClusterClient.ListTables();
            Context.Logger.Info("HBase Tables (" + tables.name.Count + "): " + String.Join(", ", tables.name));


            //Create a HBase table if it not exists
            if (!tables.name.Contains(this.HBaseTableName))
            {
                throw new Exception("Cannot find HBase table: " + this.HBaseTableName);
            }
        }

        /// <summary>
        /// Executes incoming tuples
        /// </summary>
        /// <param name="tuple">The first field is treated as rowkey and rest as column names</param>
        public void Execute(SCPTuple tuple)
        {
            try
            {
                //TODO: Change the HBase scanning criteria as per your needs
                //filter = new PrefixFilter(ToBytes(tuple.GetValue(0)))
                //Or, use a different field for end scan like: endRow = ToBytes(tuple.GetValue(1))
                var scannersettings = new Scanner()
                {
                    startRow = ToBytes(tuple.GetValue(0)),
                    endRow = ToBytes(tuple.GetValue(0)),
                };

                var scannerInfo = HBaseClusterClient.CreateScanner(this.HBaseTableName, scannersettings);
                CellSet readSet = null;

                while ((readSet = HBaseClusterClient.ScannerGetNext(scannerInfo)) != null)
                {
                    Context.Logger.Info("Rows found: {0}", readSet.rows.Count);
                    foreach (var row in readSet.rows)
                    {
                        var emitValues = new List<object>();
                        //TODO: You can choose to emit the row key along with the values
                        emitValues.Add(Encoding.UTF8.GetString(row.key));

                        //Add the values from the readSet
                        //TODO: The byte[] from HBase can be any type, make sure you type cast it correctly before emitting
                        //The code below only handles strings
                        emitValues.AddRange(row.values.Select(v => Encoding.UTF8.GetString(v.data)));
                        Context.Logger.Info("Rowkey: {0}, Values: {1}",
                            Encoding.UTF8.GetString(row.key), String.Join(", ", row.values.Select(v => Encoding.UTF8.GetString(v.data))));

                        if (enableAck)
                        {
                            this.context.Emit(Constants.DEFAULT_STREAM_ID, new List<SCPTuple>() { tuple }, emitValues);
                        }
                        else
                        {
                            this.context.Emit(Constants.DEFAULT_STREAM_ID, emitValues);
                        }
                    }
                }

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

        static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetDateTimeInMillis(DateTime dateTime)
        {
            //TODO - OPTIONAL - Use this block if you dateTime is not UTC
            /*
            if(dateTime.Kind != DateTimeKind.Utc)
            {
                dateTime = dateTime.ToUniversalTime();
            }
            */
            return (long)(dateTime - Jan1st1970).TotalMilliseconds;
        }

        /// <summary>
        /// Converts primitive types to byte array
        /// </summary>
        /// <param name="o">input object of primitive type</param>
        /// <returns>byte array representing the input object</returns>
        public static byte[] ToBytes(object o)
        {
            if (o is string)
            {
                return Encoding.UTF8.GetBytes((string)o);
            }
            else if (o is int)
            {
                return BitConverter.GetBytes((int)o);
            }
            else if (o is long)
            {
                return BitConverter.GetBytes((long)o);
            }
            else if (o is short)
            {
                return BitConverter.GetBytes((short)o);
            }
            else if (o is double)
            {
                return BitConverter.GetBytes((double)o);
            }
            else if (o is float)
            {
                return BitConverter.GetBytes((float)o);
            }
            else if (o is bool)
            {
                return BitConverter.GetBytes((bool)o);
            }
            else if (o is DateTime)
            {
                return BitConverter.GetBytes(GetDateTimeInMillis((DateTime)o)); //We can store the DateTime as Millis since epoch
            }
            else
            {
                throw new NotImplementedException("ToBytes() can only handle primitive types. To use other types, please modify the implementation or change input schema.");
            }
        }
    }
}
