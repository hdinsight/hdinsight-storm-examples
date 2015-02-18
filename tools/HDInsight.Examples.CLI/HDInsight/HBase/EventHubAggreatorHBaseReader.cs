using log4net;
using Microsoft.HBase.Client;
using org.apache.hadoop.hbase.rest.protobuf.generated;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace HDInsight.Examples.CLI
{
    /// <summary>
    /// A simple HBase reader class that also provides aggregation method on fetch
    /// </summary>
    public class HBaseReaderClient
    {
        static readonly ILog LOG = LogManager.GetLogger(typeof(HBaseReaderClient));

        public static string KEY_DELIMITER = "#";
        public static string DATE_TIME_FORMAT = "yyyyMMddHHmmss";

        string HBaseClusterUrl;
        string HBaseClusterUserName;
        string HBaseClusterUserPassword;

        ClusterCredentials HBaseClusterCredentials;
        HBaseClient HBaseClusterClient;

        public HBaseReaderClient(string hbaseclusterurl, string hbaseclusterusername, string hbaseclusteruserpassword)
        {
            this.HBaseClusterUrl = hbaseclusterurl;
            this.HBaseClusterUserName = hbaseclusterusername;
            this.HBaseClusterUserPassword = hbaseclusteruserpassword;

            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;

            this.HBaseClusterCredentials = new ClusterCredentials(new Uri(HBaseClusterUrl), HBaseClusterUserName, HBaseClusterUserPassword);
            this.HBaseClusterClient = new HBaseClient(HBaseClusterCredentials);
        }

        public Dictionary<string, Dictionary<string, double>> ScanHBase(string hbasetablename, string startkey, string endkey)
        {
            var scannersettings = new Scanner()
            {
                startRow = Encoding.UTF8.GetBytes(startkey),
                endRow = Encoding.UTF8.GetBytes(endkey),
            };

            var localstopwatch = new Stopwatch();
            localstopwatch.Start();

            var hbaseresultset = new Dictionary<string, Dictionary<string, double>>();


            var scannerInfo = HBaseClusterClient.CreateScanner(hbasetablename, scannersettings);
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

            var overallresult = new Dictionary<string, double>();

            LOG.InfoFormat("ScanHBase: {0} - Time Taken = {1} ms", hbasetablename, localstopwatch.ElapsedMilliseconds);
            return hbaseresultset;
        }

        public List<KeyValuePair<string, double>> AggregateAndOrderHBaseResultSetByValues(Dictionary<string, Dictionary<string, double>> hbaseresultset, int topN = -1)
        {
            var overallresult = new Dictionary<string, double>();
            foreach (var rowkey in hbaseresultset.Keys)
            {
                var sortedcolumns = hbaseresultset[rowkey].OrderByDescending(c => c.Value).Take(15).ToList();
                foreach (var column in sortedcolumns)
                {
                    if (overallresult.ContainsKey(column.Key))
                    {
                        overallresult[column.Key] += column.Value;
                    }
                    else
                    {
                        overallresult.Add(column.Key, column.Value);
                    }
                }
            }

            if (topN > 0)
            {
                return overallresult.OrderByDescending(r => r.Value).Take(topN).ToList();
            }

            return overallresult.OrderByDescending(r => r.Value).ToList();
        }

        public static void MonitorHBase()
        {
            LOG.DebugFormat("Monitoring records in HBase: {0}. Use 'Ctrl-C' to break out of the application.", 
                    HDInsightHelper.HBaseCluster.Name);
                    var hbClient = new HBaseReaderClient(
                        HDInsightHelper.HBaseCluster.ConnectionUrl,
                        HDInsightHelper.HBaseCluster.HttpUserName,
                        HDInsightHelper.HBaseCluster.HttpPassword);
            
            List<string> RandomResponses = new List<string>() { 
                HttpStatusCode.OK.ToString(),
                HttpStatusCode.BadRequest.ToString(),
                HttpStatusCode.NotFound.ToString(),
                HttpStatusCode.InternalServerError.ToString()};

            while (true)
            {
                foreach (var response in RandomResponses)
                {
                    var dateTime = DateTime.UtcNow;
                    var startKey = response + KEY_DELIMITER +
                        dateTime.AddMinutes(-30).Floor().ToString(DATE_TIME_FORMAT);
                    var endKey = response + KEY_DELIMITER +
                        dateTime.Floor().ToString(DATE_TIME_FORMAT);

                    //TODO - Change table name according to aggregation primary or secondary key
                    var tableName = "EHResultClientTable";

                    LOG.InfoFormat("Scanning HBase Aggreagations - " +
                        "Table: {0}, StartKey: {1}, EndKey: {2}", tableName, startKey, endKey);
                    var records = hbClient.AggregateAndOrderHBaseResultSetByValues(
                        hbClient.ScanHBase(tableName,
                        startKey,
                        endKey));

                    if (records.Count > 0)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine();
                        sb.AppendLine(String.Format("Table: {0}, StartKey: {1}, EndKey: {2}",
                            tableName, startKey, endKey));
                        foreach (var r in records)
                        {
                            sb.AppendLine(String.Format("Column = {0}, AggreagatedValue = {1}", r.Key, r.Value));
                        }
                        sb.AppendLine();
                        LOG.InfoFormat(sb.ToString());
                    }
                    else
                    {
                        LOG.Info("No records found!");
                    }
                }
                Thread.Sleep(TimeSpan.FromMinutes(1));
            }
        }
    }
}
