using EventHubAggregatorToHBaseTopology.Common;
using Microsoft.HBase.Client;
using Microsoft.HBase.Client.Filters;
using Microsoft.SCP;
using org.apache.hadoop.hbase.rest.protobuf.generated;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EventHubAggregatorToHBaseTopology.Bolts
{
    /// <summary>
    /// This is HBase writer bolt that uses the HBase .Net SDK to write aggregated tuple into HBase
    /// Each RowKey is a PrimaryKey#Timestamp, ColumnName is the SecondaryKey, and the Cell Value is the aggregated Value.
    /// </summary>
    class EventHBaseWriter : EventReAggregator
    {
        public static ClusterCredentials HBaseClusterCredentials;
        public HBaseClient HBaseClusterClient;

        public string HBaseTableName { get; set; }

        public int global_hbasescan_count = 0;
        public int local_hbasescan_count = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tablename"></param>
        public EventHBaseWriter(Context context, Dictionary<string, Object> parms = null)
        {
            this.context = context;

            this.appConfig = new AppConfig();

            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, AggregatedTupleFields);

            this.context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, null));

            //Setup the credentials for HBase cluster
            HBaseClusterCredentials = 
                new ClusterCredentials(
                    new Uri(this.appConfig.HBaseClusterUrl), 
                    this.appConfig.HBaseClusterUserName, 
                    this.appConfig.HBaseClusterUserPassword);

            HBaseClusterClient = new HBaseClient(HBaseClusterCredentials);

            //Query HBase for existing tables
            var tabs = HBaseClusterClient.ListTables();
            Context.Logger.Info("HBase Tables (" + tabs.name.Count + "): " + String.Join(", ", tabs.name));

            this.PrimaryKey = this.appConfig.PrimaryKey;
            this.SecondaryKey = this.appConfig.SecondaryKey;

            this.HBaseTableName =
                this.appConfig.HBaseTableNamePrefix +
                this.appConfig.PrimaryKey + this.appConfig.SecondaryKey +
                this.appConfig.HBaseTableNameSuffix;

            Context.Logger.Info("HBaseTableName = " + this.HBaseTableName);

            //Create a HBase table if it not exists
            if (!tabs.name.Contains(this.HBaseTableName))
            {
                var tableSchema = new TableSchema();
                tableSchema.name = this.HBaseTableName;
                tableSchema.columns.Add(new ColumnSchema() { name = "v" });
                HBaseClusterClient.CreateTable(tableSchema);
                Context.Logger.Info("Created HBase Table: " + this.HBaseTableName);
            }

            Context.Logger.Info("HBaseOverwrite: " + this.appConfig.HBaseOverwrite);

            globalstopwatch = new Stopwatch();
            globalstopwatch.Start();

            emitstopwatch = new Stopwatch();
            emitstopwatch.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tuple"></param>
        public override void Execute(SCPTuple tuple)
        {
            ProcessTuple(tuple);
        }

        /// <summary>
        /// Create a write set for HBase when the emit period completes
        /// We delay all the writes to batch the aggregations and writes for them
        /// </summary>
        /// <returns></returns>
        public override bool EmitAggregations()
        {
            try
            {
                if (emitstopwatch.Elapsed > this.appConfig.AggregationWindow)
                {
                    local_hbasescan_count = 0;
                    var writeset = new CellSet();
                    var emitime = DateTime.UtcNow.Floor(this.appConfig.AggregationWindow).Subtract(this.appConfig.EmitWindow);
                    var keystoremove = new List<DateTime>();

                    var dtkeys = aggregatedCounts.Keys.Where(dt => dt < emitime).OrderBy(dt => dt).ToList();

                    var hbaseresultsets = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();

                    if (dtkeys.Count > 0)
                    {
                        var startdt = dtkeys[0];
                        var enddt = dtkeys[dtkeys.Count-1];

                        foreach (var dtkey in dtkeys)
                        {
                            foreach (var pkey in aggregatedCounts[dtkey].Keys)
                            {
                                if (!this.appConfig.HBaseOverwrite && !hbaseresultsets.ContainsKey(pkey))
                                {
                                    hbaseresultsets.Add(pkey, 
                                        ScanHBase(
                                        pkey + Utilities.KEY_DELIMITER + startdt.ToString(Utilities.DATE_TIME_FORMAT),
                                        pkey + Utilities.KEY_DELIMITER + enddt.ToString(Utilities.DATE_TIME_FORMAT)));
                                }

                                var rowkey = pkey + Utilities.KEY_DELIMITER + dtkey.ToString(Utilities.DATE_TIME_FORMAT);
                                var tablerow = new CellSet.Row { key = Encoding.UTF8.GetBytes(rowkey) };
                                foreach (var skey in aggregatedCounts[dtkey][pkey].Keys)
                                {
                                    var columnkey = "v:" + skey;
                                    double previousdata = 0;

                                    if (hbaseresultsets.ContainsKey(pkey) && 
                                        hbaseresultsets[pkey].ContainsKey(rowkey) && 
                                        hbaseresultsets[pkey][rowkey].ContainsKey(columnkey))
                                    {
                                        previousdata = hbaseresultsets[pkey][rowkey][columnkey];
                                    }

                                    var rowcell = new Cell
                                    {
                                        column = Encoding.UTF8.GetBytes(columnkey),
                                        data = BitConverter.GetBytes(previousdata + aggregatedCounts[dtkey][pkey][skey])
                                    };

                                    tablerow.values.Add(rowcell);
                                    global_emit_count++;
                                    last_emit_count++;
                                    current_cache_size--;
                                }
                                writeset.rows.Add(tablerow);
                            }
                            keystoremove.Add(dtkey);
                        }
                    }

                    if (writeset != null && writeset.rows.Count > 0)
                    {
                        Context.Logger.Info("HBaseTableName: {0} - Rows to write: {1}, First Rowkey = {2}", 
                            this.HBaseTableName, writeset.rows.Count, Encoding.UTF8.GetString(writeset.rows[0].key));
                        
                        var localstopwatch = new Stopwatch();
                        localstopwatch.Start();
                        //Use the StoreCells API to write the cellset into HBase Table
                        HBaseClusterClient.StoreCells(this.HBaseTableName, writeset);
                        Context.Logger.Info("HBase: Table = {0}, Rows Written = {1}, Write Time = {2} secs, Time since last write = {3} secs",
                            this.HBaseTableName, writeset.rows.Count, localstopwatch.Elapsed.TotalSeconds, emitstopwatch.Elapsed.TotalSeconds);

                        foreach (var key in keystoremove)
                        {
                            aggregatedCounts.Remove(key);
                        }
                        last_emit_in_secs = emitstopwatch.Elapsed.TotalSeconds;
                        emitstopwatch.Restart();
                    }

                    if (!this.appConfig.HBaseOverwrite)
                    {
                        Context.Logger.Info("ScanHBase: Last Window Scan Count = {0}, Table = {1}", local_hbasescan_count, this.HBaseTableName);
                    }
                }

                if (last_emit_count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                last_error_count++;
                global_error_count++;
                Context.Logger.Error(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public new static EventHBaseWriter Get(Context context, Dictionary<string, Object> parms)
        {
            return new EventHBaseWriter(context, parms);
        }

        /// <summary>
        /// Scan the HBase Table for a give start row key and end row key
        /// You can swap out the scanner for a differnt type of filters like PrefixFilter
        /// Read more about HBase Filters here: http://hbase.apache.org/book/client.filter.html
        /// You can also refer to HBase SDK examples here on how each filter is used: https://github.com/hdinsight/hbase-sdk-for-net/tree/master/Microsoft.HBase.Client.Tests
        /// </summary>
        /// <param name="startrowkey"></param>
        /// <param name="endrowkey"></param>
        /// <returns></returns>
        public Dictionary<string, Dictionary<string, double>> ScanHBase(string startrowkey, string endrowkey)
        {
            global_hbasescan_count++;
            local_hbasescan_count++;

            if (global_hbasescan_count % 1000 == 0)
            {
                Context.Logger.Info("ScanHBase: Global Scan Count = {0}, Table = {1}, StartRowKey = {2}, EndRowKey = {3}",
                    global_hbasescan_count, this.HBaseTableName, startrowkey, endrowkey);
            }

            var scannersettings = new Scanner()
            {
                startRow = Encoding.UTF8.GetBytes(startrowkey),
                endRow = Encoding.UTF8.GetBytes(endrowkey)
            };

            var hbaseresultset = new Dictionary<string, Dictionary<string, double>>();

            try
            {
                var scannerInfo = HBaseClusterClient.CreateScanner(this.HBaseTableName, scannersettings);
                CellSet readset = null;

                while ((readset = HBaseClusterClient.ScannerGetNext(scannerInfo)) != null)
                {
                    foreach (var row in readset.rows)
                    {
                        var rowkey = Encoding.UTF8.GetString(row.key);
                        if (hbaseresultset.ContainsKey(rowkey))
                        {
                            foreach (var column in row.values)
                            {
                                var columnkey = Encoding.UTF8.GetString(column.column);
                                var value = BitConverter.ToDouble(column.data, 0);

                                if (hbaseresultset[rowkey].ContainsKey(columnkey))
                                {
                                    hbaseresultset[rowkey][columnkey] += value;
                                }
                                else
                                {
                                    hbaseresultset[rowkey].Add(columnkey, value);
                                }
                            }
                        }
                        else
                        {
                            var newresult = new Dictionary<string, double>();
                            foreach (var column in row.values)
                            {
                                var columnkey = Encoding.UTF8.GetString(column.column);
                                var value = BitConverter.ToDouble(column.data, 0);

                                if (newresult.ContainsKey(columnkey))
                                {
                                    newresult[columnkey] += value;
                                }
                                else
                                {
                                    newresult.Add(columnkey, value);
                                }
                            }
                            hbaseresultset.Add(rowkey, newresult);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Context.Logger.Error("ScanHBase Failed: Table = {0}, StartRowKey = {1}, EndRowKey = {2}", this.HBaseTableName, startrowkey, endrowkey);
                throw;
            }
            return hbaseresultset;
        }
    }
}
