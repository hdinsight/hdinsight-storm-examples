using org.apache.hadoop.hbase.rest.protobuf.generated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EventHubAggregatorToHBaseTopology.Common
{
    public static class Utilities
    {
        public static string DEFAULT_VALUE = "null";
        public static string KEY_DELIMITER = "#";
        public static string DATE_TIME_FORMAT = "yyyyMMddHHmmss";

        public static string UrlEncodeString(string s)
        {
            return HttpUtility.UrlEncode(s, Encoding.UTF8);
        }
    }

    public static class HBaseExtensions
    {
        public static Dictionary<byte[], Dictionary<byte[], byte[]>> ToDictionary(this CellSet cellset)
        {
            if (cellset == null || cellset.rows == null)
            {
                return new Dictionary<byte[],Dictionary<byte[],byte[]>>();
            }

            return cellset.rows.ToDictionary(row => row.key, row => row.values.ToDictionary(value => value.column, value => value.data));
        }
    }
}