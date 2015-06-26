using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace EventHubAggregatorToHBaseTopology.Common
{
    public class AppConfig
    {
        public string TimestampField { get; set; }

        public TimeSpan AggregationWindow { get; set; }
        public TimeSpan EmitWindow { get; set; }

        public TimeSpan PurgeWindow { get; set; }

        public int AggregationRankerTopNCount { get; set; }

        public string EventHubFqnAddress { get; set; }
        public string EventHubNamespace { get; set; }
        public string EventHubEntityPath { get; set; }
        public string EventHubSharedAccessKeyName { get; set; }
        public string EventHubSharedAccessKey { get; set; }
        public int EventHubPartitions { get; set; }

        public string HBaseClusterUrl { get; set; }
        public string HBaseClusterUserName { get; set; }
        public string HBaseClusterUserPassword { get; set; }

        public string HBaseTableNamePrefix { get; set; }
        public string HBaseTableNameSuffix { get; set; }

        public bool HBaseOverwrite { get; set; }

        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }

        public AppConfig()
        {
            ReadValues();
        }

        private void ReadValues()
        {
            TimestampField = ConfigurationManager.AppSettings["TimestampField"];

            int window = 1;
            var result = int.TryParse(ConfigurationManager.AppSettings["AggregationWindow"], out window);
            if (result)
            {
                AggregationWindow = TimeSpan.FromMinutes(window);
            }
            else
            {
                AggregationWindow = TimeSpan.FromMinutes(1);
            }

            window = 1;
            result = int.TryParse(ConfigurationManager.AppSettings["EmitWindow"], out window);
            if (result)
            {
                EmitWindow = TimeSpan.FromMinutes(window);
            }
            else
            {
                EmitWindow = TimeSpan.FromMinutes(1);
            }

            int count = 100;
            result = int.TryParse(ConfigurationManager.AppSettings["AggregationRankerTopNCount"], out count);
            if (result)
            {
                AggregationRankerTopNCount = count;
            }
            else
            {
                AggregationRankerTopNCount = 100;
            }

            EventHubEntityPath = ConfigurationManager.AppSettings["EventHubEntityPath"];
            EventHubFqnAddress = ConfigurationManager.AppSettings["EventHubFqnAddress"];
            EventHubNamespace = ConfigurationManager.AppSettings["EventHubNamespace"];
            EventHubSharedAccessKeyName = ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"];
            EventHubSharedAccessKey = ConfigurationManager.AppSettings["EventHubSharedAccessKey"];

            var partitions = 0;
            var parseResult = int.TryParse(ConfigurationManager.AppSettings["EventHubPartitions"], out partitions);
            if (parseResult)
            {
                EventHubPartitions = partitions;
            }
            else
            {
                EventHubPartitions = 16;
            }

            HBaseClusterUrl = ConfigurationManager.AppSettings["HBaseClusterUrl"];
            HBaseClusterUserName = ConfigurationManager.AppSettings["HBaseClusterUserName"];
            HBaseClusterUserPassword = ConfigurationManager.AppSettings["HBaseClusterUserPassword"];

            HBaseTableNamePrefix = ConfigurationManager.AppSettings["HBaseTableNamePrefix"];
            HBaseTableNameSuffix = ConfigurationManager.AppSettings["HBaseTableNameSuffix"];

            var hbaseoverwrite = true;
            result = bool.TryParse(ConfigurationManager.AppSettings["HBaseOverwrite"], out hbaseoverwrite);
            if (result)
            {
                HBaseOverwrite = hbaseoverwrite;
            }
            else
            {
                HBaseOverwrite = true;
            }

            PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];
            SecondaryKey = ConfigurationManager.AppSettings["SecondaryKey"];
        }
    }
}