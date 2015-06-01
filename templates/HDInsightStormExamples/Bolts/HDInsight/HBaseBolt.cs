using HDInsightStormExamples.Spouts;
using Microsoft.HBase.Client;
using Microsoft.SCP;
using org.apache.hadoop.hbase.rest.protobuf.generated;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace HDInsightStormExamples.Bolts
{
    /// <summary>
    /// OVERVIEW:
    /// This is a HBase bolt that uses the HBase .Net SDK to write incoming rows into HBase.
    /// It batches the writes for better throughput. The batches can be emitted based on count or a set Tick tuple frequency.
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
    class HBaseBolt : ISCPBolt
    {
        Context context;
        bool enableAck = false;

        string HBaseClusterUrl { get; set; }
        string HBaseClusterUserName { get; set; }
        string HBaseClusterPassword { get; set; }

        ClusterCredentials HBaseClusterCredentials;
        HBaseClient HBaseClusterClient;

        string HBaseTableName { get; set; }
        string HBaseTableColumnFamily { get; set;}
        List<string> HBaseTableColumns { get; set;}

        long seqId = 0;
        Dictionary<long, SCPTuple> cachedTuples = new Dictionary<long, SCPTuple>();

        /// <summary>
        /// HBaseBolt constructor
        /// </summary>
        /// <param name="context"></param>
        public HBaseBolt(Context context)
        {
            //Set the context
            this.context = context;

            //TODO: VERY IMPORTANT - Declare the schema for the incoming tuples from the downstream spout or bolt tasks
            //You will also need to declare the schema for the any outgoing tuples to the upstream spout or bolt tasks
            //If there are no outgoing tuples, you can set outputSchema to null in ComponentStreamSchema
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();

            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string), typeof(DateTime), typeof(string), typeof(int), typeof(int), typeof(string) });
            //Another way is to have the OutputFieldTypes list exposed from Spout or Bolt to make it easy to tie with bolts that will consume it
            //inputSchema.Add(Constants.DEFAULT_STREAM_ID, IISLogGeneratorSpout.OutputFieldTypes);
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpout.OutputFieldTypes);

            //TODO: Choose if you wish to use Tick tuples - By default we are listening to the Tick tuple stream
            inputSchema.Add(Constants.SYSTEM_TICK_STREAM_ID, new List<Type> { typeof(long) });

            //Declare both input and output schemas
            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            //TODO: Uncomment if using in hybrid mode (Java -> C#) - We need to Deserialize Java objects into C# objects (using JSON)
            //Do NOT forget to declare the serializer in you TopologyBuilder for this bolt
            //  set: DeclareCustomizedJavaSerializer(new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" } )
            //If the incoming tuples have only string fields, this is optional
            //##UNCOMMENT_THIS_LINE##//this.context.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

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
        public static HBaseBolt Get(Context context, Dictionary<string, Object> parms)
        {
            return new HBaseBolt(context);
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

            this.HBaseTableColumnFamily = ConfigurationManager.AppSettings["HBaseTableColumnFamily"];
            if (String.IsNullOrWhiteSpace(this.HBaseTableColumnFamily))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "HBaseTableColumnFamily");
            }

            //TODO - DO NOT include the ROWKEY field in the column list as it is assumed to be at index 0 in tuple fields
            //Rest of the tuple fields are mapped with the column names provided
            var hbaseTableColumnNames = ConfigurationManager.AppSettings["HBaseTableColumnNames"];
            if (String.IsNullOrWhiteSpace(hbaseTableColumnNames))
            {
                throw new ArgumentException("A required AppSetting cannot be null or empty", "HBaseTableColumnNames");
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
            Context.Logger.Info("Existing HBase tables - Count: {0}, Tables: {1}", tables.name.Count, String.Join(", ", tables.name));

            //Read the HBaseTable Columns and convert them to byte[]
            this.HBaseTableColumns = hbaseTableColumnNames.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).
                Select(c => c.Trim()).ToList();

            //Create a HBase table if it not exists
            if (!tables.name.Contains(this.HBaseTableName))
            {
                var tableSchema = new TableSchema();
                tableSchema.name = this.HBaseTableName;
                tableSchema.columns.Add(new ColumnSchema() { name = this.HBaseTableColumnFamily });
                HBaseClusterClient.CreateTable(tableSchema);
                Context.Logger.Info("Created HBase table: {0}", this.HBaseTableName);
            }
            else
            {
                Context.Logger.Info("Found an existing HBase table: {0}", this.HBaseTableName);
            }
        }

        /// <summary>
        /// Executes incoming tuples
        /// </summary>
        /// <param name="tuple">The first field is treated as rowkey and rest as column values</param>
        public void Execute(SCPTuple tuple)
        {
            try
            {
                var isTickTuple = tuple.GetSourceStreamId().Equals(Constants.SYSTEM_TICK_STREAM_ID);

                //Only add to cache if its not a Tick tuple
                if (!isTickTuple)
                {
                    //seqId helps in keeping the incoming tuples in order of their arrival
                    cachedTuples.Add(seqId, tuple);
                    seqId++;
                }

                //TODO: You can choose to write into HBase based on cached tuples count or when the tick tuple arrives
                //To use Tick tuples make sure that you configure topology.tick.tuple.freq.secs on the bolt and also add the stream in the input streams
                /* Add this section to your SetBolt in TopologyBuilder to trigger Tick tuples
                addConfigurations(new Dictionary<string, string>()
                {
                    {"topology.tick.tuple.freq.secs", "5"}
                })
                */
                if (cachedTuples.Count >= 1000 || isTickTuple)
                {
                    WriteToHBase();
                    //Ack the tuple if enableAck is set to true in TopologyBuilder. This is mandatory if the downstream bolt or spout expects an ack.
                    if (enableAck)
                    {
                        //Ack all the tuples in the batch
                        foreach(var cachedTuple in cachedTuples)
                        {
                            this.context.Ack(cachedTuple.Value);
                        }
                    }
                    cachedTuples.Clear();
                }
            }
            catch (Exception ex)
            {
                Context.Logger.Error("An error occured while executing Tuple Id: {0}. Exception Details:\r\n{1}",
                    tuple.GetTupleId(), ex.ToString());

                if (enableAck)
                {
                    Context.Logger.Error("Failing the entire current batch");
                    //Fail all the tuples in the batch
                    foreach (var cachedTuple in cachedTuples)
                    {
                        this.context.Fail(cachedTuple.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Create a write set for HBase based on cached tuples
        /// We delay all the writes to batch the aggregations and writes for them
        /// </summary>
        /// <returns></returns>
        public void WriteToHBase()
        {
            Context.Logger.Info("WriteToHBase - Start - Writing cached rows into HBase. Rows to write: {0}", cachedTuples.Count);
            if (cachedTuples.Count > 0)
            {
                var writeSet = new CellSet();
                foreach (var cachedTuple in cachedTuples)
                {
                    var values = cachedTuple.Value.GetValues();
                    if(this.HBaseTableColumns.Count < (values.Count - 1))
                    {
                        throw new Exception(String.Format(
                            "Count of HBaseTableColumns is less than fields received. HBaseTableColumns.Count: {0}, Values.Count (without rowkey): {1}",
                            this.HBaseTableColumns.Count, values.Count - 1)
                            );
                    }

                    //Use the first value as rowkey and add the remaining as a list
                    var tablerow = new CellSet.Row { key = ToBytes(values[0]) };

                    //Skip the first value and read the remaining
                    for (int i = 1; i < values.Count; i++)
                    {
                        var rowcell = new Cell
                        {
                            //Based on our assumption that ColumnNames do NOT contain rowkey field
                            column = ToBytes(this.HBaseTableColumnFamily + ":" + this.HBaseTableColumns[i-1]),
                            data = ToBytes(values[i])
                        };
                        tablerow.values.Add(rowcell);
                    }

                    writeSet.rows.Add(tablerow);
                }

                try
                {
                    //Use the StoreCells API to write the cellset into HBase Table
                    HBaseClusterClient.StoreCells(this.HBaseTableName, writeSet);
                }
                catch
                {
                    Context.Logger.Error("HBase StoreCells Failed");
                    foreach(var row in writeSet.rows)
                    {
                        Context.Logger.Info("Failed RowKey: {0}, Values (bytes): {1}", Encoding.UTF8.GetString(row.key),
                            String.Join(", ", row.values.Select(v => Encoding.UTF8.GetString(v.column) + " = " + v.data.LongLength)));
                    }
                    throw;
                }
                Context.Logger.Info("WriteToHBase - End - Stored cells into HBase. Rows written: {0}", writeSet.rows.Count);
            }
            else
            {
                Context.Logger.Info("WriteToHBase - End - No cells to write.");
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
            else if(o is DateTime)
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
